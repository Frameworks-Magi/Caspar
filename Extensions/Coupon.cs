using Framework.Caspar;
using Framework.Caspar.Container;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices.ComTypes;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using dbms = Framework.Caspar.Database.Management;
using static Framework.Caspar.Extensions.Database;

namespace Framework.Caspar
{

	static public partial class Api
	{
		public static partial class Coupon
		{
			// 0, 1, O, I 제외
			static char[] Table = { '2', '3', '4', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'J', 'K', 'L', 'M', 'N', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z' };
			public static byte[] Key { get; set; }
			public static byte[] IV { get; set; }
			static private int GetValue(char c)
			{

				for (int i = 0; i < Table.Length; ++i)
				{
					if (Table[i] == c)
					{
						return i;
					}
				}

				return -1;
			}

			public static string DB { get; set; } = "Game";


			private static ConcurrentDictionary<string, byte[]> Keys = new ConcurrentDictionary<string, byte[]>();
			private static ConcurrentDictionary<(int, long), Info> Metadatas = new ConcurrentDictionary<(int, long), Info>();


			public class Info
			{
				public int Category { get; set; }
				public int Prefix { get; set; }
				public string Key { get; set; }
				public int Id { get; set; }
				public int Limit { get; set; }
				public int Type { get; set; }
				public string Reward { get; set; }
				public int Version { get; set; }
				public DateTime Expire { get; set; }
				public DateTime Timestamp { get; set; }
				public string Remark { get; set; }
			}

			private static async Task<byte[]> getKey(string prefix)
			{
				if (prefix.Length > 4) { return null; }

				var key = Keys.Get(prefix);
				if (key != null) { return key; }

				using var session = new global::Framework.Caspar.Database.Session();


				key = Keys.Get(prefix);

				if (key == null)
				{
					var connection = await session.GetConnection(DB);
					var command = connection.CreateCommand();
					command.CommandText = $"SELECT * FROM `caspar`.`CouponInfo` WHERE Prefix = {prefix.ToCouponPrefixCode()} LIMIT 1";
					var result = command.ExecuteReader().ToResultSet();

					if (result[0].Count == 0)
					{
						return null;
					}
					else
					{


						key = ASCIIEncoding.ASCII.GetBytes(result[0][0][1].ToString());
						Keys.AddOrUpdate(prefix, key);
					}
				}
				return Keys.Get(prefix);
			}

			private static async Task<Info> getMetadata(string prefix, string key)
			{
				long id = key.ToCouponPrefixCode();
				var metadata = Metadatas.Get((prefix.ToCouponPrefixCode(), id));
				if (metadata != null) { return metadata; }

				using var session = new global::Framework.Caspar.Database.Session();

				//session.Command = async () =>
				var connection = await session.GetConnection(DB);
				var command = connection.CreateCommand();
				command.CommandText = $"SELECT * FROM `caspar`.`CouponInfo` WHERE Prefix = {prefix.ToCouponPrefixCode()} AND Key = {key};";
				var result = command.ExecuteReader().ToResultSet();
				if (result[0].Count == 0)
				{
					return null;
				}


				Metadatas.AddOrUpdate((prefix.ToCouponPrefixCode(), id), new Info()
				{
					Prefix = result[0][0][0].ToInt32(),
					Id = result[0][0][2].ToInt32(),
					Limit = result[0][0][3].ToInt32(),
					Category = result[0][0][4].ToInt32(),
					Type = result[0][0][5].ToInt32(),
					Reward = result[0][0][7].ToString(),
					Version = result[0][0][8].ToInt32(),
					Expire = result[0][0][9].ToDateTime(),
					Timestamp = result[0][0][10].ToDateTime(),
				});
				return Metadatas.Get((prefix.ToCouponPrefixCode(), id));
			}
			private static async Task<Info> getMetadata(string prefix, int id)
			{
				var metadata = Metadatas.Get((prefix.ToCouponPrefixCode(), id));
				if (metadata != null) { return metadata; }

				using var session = new global::Framework.Caspar.Database.Session();

				var connection = await session.GetConnection(DB);
				var command = connection.CreateCommand();
				command.CommandText = $"SELECT * FROM `caspar`.`CouponInfo` WHERE Prefix = {prefix.ToCouponPrefixCode()} AND Id = {id};";
				var result = command.ExecuteReader().ToResultSet();
				if (result[0].Count == 0)
				{
					return null;
				}


				Metadatas.AddOrUpdate((prefix.ToCouponPrefixCode(), id), new Info()
				{
					Prefix = result[0][0][0].ToInt32(),
					Key = result[0][0][1].ToString(),
					Id = result[0][0][2].ToInt32(),
					Limit = result[0][0][3].ToInt32(),
					Category = result[0][0][4].ToInt32(),
					Type = result[0][0][5].ToInt32(),
					Reward = result[0][0][7].ToString(),
					Version = result[0][0][8].ToInt32(),
					Expire = result[0][0][9].ToDateTime(),
					Timestamp = result[0][0][10].ToDateTime(),
				});


				var key = ASCIIEncoding.ASCII.GetBytes(result[0][0][1].ToString());
				Keys.AddOrUpdate(prefix, key);
				return Metadatas.Get((prefix.ToCouponPrefixCode(), id));
			}

			public static async Task<List<string>> GetCoupons(string prefix, ushort index, ushort count)
			{

				using var session = new global::Framework.Caspar.Database.Session();

				var key = await getKey(prefix);

				var connection = await session.GetConnection(DB);
				var command = connection.CreateCommand();
				command.CommandText = $"SELECT * FROM `caspar`.`CouponInfo` WHERE Prefix = {prefix.ToCouponPrefixCode()} AND Id = {index};";
				var result = command.ExecuteReader().ToResultSet();
				if (result[0].Count == 0)
				{
					return null;
				}

				session.ResultSet = new
				{
					Prefix = result[0][0][0].ToInt32(),
					Key = result[0][0][1].ToString(),
					Id = result[0][0][2].ToInt32(),
					Limit = result[0][0][3].ToInt32(),
					Category = result[0][0][4].ToInt32(),
					Type = result[0][0][5].ToInt32(),
					Consume = result[0][0][6].ToInt32(),
					Reward = result[0][0][7].ToString(),
					Version = result[0][0][8].ToInt32(),
					Expire = result[0][0][9].ToDateTime(),
					Timestamp = result[0][0][10].ToDateTime(),
				};


				if ((int)session.ResultSet.Id != index) { return null; }
				if ((int)session.ResultSet.Limit < count) { return null; }
				return generate(key, index, count);

			}
			private static List<string> generate(byte[] key, ushort index, ushort count)
			{
				List<string> coupons = new List<string>();

				for (ushort i = 0; i < count; ++i)
				{

					uint value = (uint)(index) << 16 | i;

					var des = System.Security.Cryptography.DES.Create();
					des.Key = key;
					des.IV = key;
					ICryptoTransform desencrypt = des.CreateEncryptor();

					byte[] encripted;
					using (MemoryStream msEncrypt = new MemoryStream())
					{
						using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, desencrypt, CryptoStreamMode.Write))
						{
							using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
							{
								swEncrypt.Write(value);
							}
							encripted = msEncrypt.ToArray();
							if (encripted.Length > 8) { throw new Exception(); }
						}
					}

					string coupon = "";
					for (int j = 0; j < 8; ++j)
					{

						byte c = encripted[j];

						char h = (char)(c / Table.Length);
						char l = (char)(c % Table.Length);

						coupon += Table[h];
						coupon += Table[l];

					}

					coupons.Add(coupon);
				}


				return coupons;
			}
			public static async Task<Info> Decrypt(string code)
			{
				code = code.Replace("-", "");
				char[] couponKey = code.ToCharArray();
				byte[] buffer = new byte[8];


				string prefix = "";
				for (int i = 0; i < 4; ++i)
				{
					prefix += couponKey[i];
				}

				Info metadata = null;
				if (code.Length < 20)
				{
					metadata = await getMetadata(prefix, code.Remove(0, 4));
					if (metadata == null) { return null; }
					if (metadata.Type != 0) { return null; }
					return metadata;
				}

				for (int i = 4; i < 20; i += 2)
				{

					char c = couponKey[i];
					int h = GetValue(c);

					c = couponKey[i + 1];
					int l = GetValue(c);

					byte v = (byte)(h * Table.Length + l);
					buffer[(i - 4) / 2] = v;
				}


				var key = await getKey(prefix);
				if (key == null) { return null; }

				var des = System.Security.Cryptography.DES.Create();
				des.Key = key;
				des.IV = key;

				string plaintext;
				try
				{
					ICryptoTransform desdecrypt = des.CreateDecryptor();
					using (MemoryStream msDecrypt = new MemoryStream(buffer))
					{
						using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, desdecrypt, CryptoStreamMode.Read))
						{
							using (StreamReader srDecrypt = new StreamReader(csDecrypt))
							{
								plaintext = srDecrypt.ReadToEnd();
							}
						}
					}
				}
				catch
				{
					return null;
				}

				uint value = Convert.ToUInt32(plaintext);

				var index = (ushort)(value >> 16);
				var count = (ushort)(value & 0x0000FFFF);
				metadata = await getMetadata(prefix, index);

				if (metadata == null) { return null; }
				if (metadata.Limit < count) { return null; }
				return metadata;

			}


			public static async Task<Info> Regist(string prefix, string key, Info info)
			{

				byte[] _key = await getKey(prefix);
				if (_key != null)
				{
					key = Encoding.ASCII.GetString(_key);
				}
				if (key.Length != 8) { return null; }

				if (await getMetadata(prefix, info.Id) != null) { return null; }
				info.Key = key;
				using var session = new Database.Session();


				var connection = await session.GetConnection(DB);
				var command = connection.CreateCommand();

				command.CommandText = $"INSERT INTO `caspar`.`CouponInfo` (Prefix, `Key`, `Id`, `Limit`, `Category`, `Type`, `Consume`, `Reward`, `Version`, `Expire`, `Timestamp`, `Remark`) VALUES ";
				command.CommandText += $"({prefix.ToCouponPrefixCode()}, '{info.Key}', {info.Id}, {info.Limit}, {info.Category}, {info.Type}, {0}, '{info.Reward}', {info.Version}, '{info.Expire.ToString("yyyy-MM-dd HH:mm:ss")}', '{info.Timestamp.ToString("yyyy-MM-dd HH:mm:ss")}', '{info.Remark}')";
				command.ExecuteNonQuery();

				return await getMetadata(prefix, info.Id);

			}


			public static int Consume(Database.ICommandable command, long idx, string coupon, Info metadata)
			{

				int prefix = 0;
				char[] couponKey = coupon.ToCharArray();

				for (int i = 0; i < 4; ++i)
				{
					prefix = prefix << 8;
					prefix |= couponKey[i];
				}

				if (metadata.Type == 0)
				{
					command.CommandText = $"INSERT INTO `caspar`.`PublicCoupon` (Idx, Prefix, `Key`, `Version`, `Timestamp`, Remark) VALUES ({idx}, {prefix}, {metadata.Key.ToCouponPrefixCode()}, {metadata.Version}, {DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")}, {""});";
				}
				else
				{

					int code1 = 0;
					int code2 = 0;
					int code3 = 0;
					int code4 = 0;


					for (int i = 4; i < 8; ++i)
					{
						code1 = code1 << 8;
						code1 |= couponKey[i];
					}

					for (int i = 8; i < 12; ++i)
					{
						code2 = code2 << 8;
						code2 |= couponKey[i];
					}

					for (int i = 12; i < 16; ++i)
					{
						code3 = code3 << 8;
						code3 |= couponKey[i];
					}

					for (int i = 16; i < 20; ++i)
					{
						code4 = code4 << 8;
						code4 |= couponKey[i];
					}

					command.CommandText = $"INSERT IGNORE INTO `caspar`.`ConsumedCoupon` (Prefix, `Category`, `Id`, Code1, Code2, Code3, Code4, Idx, Remark, `Version`, `Timestamp`) VALUES ({prefix}, {metadata.Category}, {metadata.Id}, {code1}, {code2}, {code3}, {code4}, {idx}, '{coupon}', {metadata.Version}, '{DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")}');";

				}

				var ret = command.ExecuteNonQuery();
				if (ret == 0)
				{
					command.CommandText = $"SELECT COUNT(*) FROM caspar.ConsumedCoupon WHERE Prefix = {prefix} AND Category = {metadata.Category} AND Idx = {idx}";
					try
					{
						if ((long)command.ExecuteScalar() == 1)
						{
							return 1; // 같은 종류의 쿠폰
						}
					}
					catch
					{

					}
					return 2; // 이미 사용된 쿠폰.
				}

				command.CommandText = $"UPDATE `caspar`.`CouponInfo` SET Consume = Consume + 1 WHERE Prefix = {prefix} AND Id = {metadata.Id};";
				ret = command.ExecuteNonQuery();
				return 0;
			}




		}
	}

}
