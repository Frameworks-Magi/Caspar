using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
//using Newtonsoft.Json.Linq;

namespace Caspar.Apple
{
    static public class Receipt
    {
        public class Result
        {
            //var str  = "{\"receipt\":{ \"original_purchase_date_pst\":\"2017-02-22 20:20:46 America/Los_Angeles\", \"purchase_date_ms\":\"1487823646055\", \"unique_identifier\":\"7b02a6a76426bfec077c65ff0235ed58b7208e0f\", \"original_transaction_id\":\"1000000276082080\", \"bvrs\":\"5\", \"transaction_id\":\"1000000276082080\", \"quantity\":\"1\", \"unique_vendor_identifier\":\"7072C350-A36C-4995-A165-03A240C8A0A5\", \"item_id\":\"1208126630\", \"product_id\":\"ttp_g0001_ios_g\", \"purchase_date\":\"2017-02-23 04:20:46 Etc/GMT\", \"original_purchase_date\":\"2017-02-23 04:20:46 Etc/GMT\", \"purchase_date_pst\":\"2017-02-22 20:20:46 America/Los_Angeles\", \"bid\":\"com.daerisoft.ttp.ios.global\", \"original_purchase_date_ms\":\"1487823646055\"}, \"status\":0}";
            //var  result = Newtonsoft.Json.JsonConvert.DeserializeObject<Caspar.Apple.Receipt.Result>(str);

            //            {"status":0, "environment":"Sandbox", "receipt":{"receipt_type":"ProductionSandbox", "adam_id":0, "app_item_id":0, "bundle_id":"com.drukhigh.ttmios", "application_version":"1.0.3", "download_id":0, "version_external_identifier":0, "receipt_creation_date":"2018-06-07 09:35:07 Etc/GMT", "receipt_creation_date_ms":"1528364107000", "receipt_creation_date_pst":"2018-06-07 02:35:07 America/Los_Angeles", "request_date":"2018-06-07 09:53:40 Etc/GMT", "request_date_ms":"1528365220391", "request_date_pst":"2018-06-07 02:53:40 America/Los_Angeles", "original_purchase_date":"2013-08-01 07:00:00 Etc/GMT", "original_purchase_date_ms":"1375340400000", "original_purchase_date_pst":"2013-08-01 00:00:00 America/Los_Angeles", "original_application_version":"1.0", 
            //            "in_app":[
            //{"quantity":"1", "product_id":"wb_ios_250", "transaction_id":"1000000405423790", "original_transaction_id":"1000000405423790", "purchase_date":"2018-06-07 09:35:07 Etc/GMT", "purchase_date_ms":"1528364107000", "purchase_date_pst":"2018-06-07 02:35:07 America/Los_Angeles", "original_purchase_date":"2018-06-07 09:35:07 Etc/GMT", "original_purchase_date_ms":"1528364107000", "original_purchase_date_pst":"2018-06-07 02:35:07 America/Los_Angeles", "is_trial_period":"false"}]}
            //}

            [Serializable]
            public class InApp
            {
                public string original_transaction_id;
                public string transaction_id;
                public string product_id;
                public string purchase_date;
                public string original_purchase_date;
                public string unique_identifier;
            }

            [Serializable]
            public class Receipt
            {
                public List<InApp> in_app;
            }

            public Receipt receipt;
            public int status;

        }

        public static async Task<Result> Verify(string receiptData, bool sandbox = false)
        {

            for (int i = 0; i < 3; ++i)
            {
                try
                {
                    // Verify the receipt with Apple
                    string postString = String.Format("{{ \"receipt-data\" : \"{0}\" }}", receiptData);
                    byte[] postBytes = Encoding.UTF8.GetBytes(postString);
                    HttpWebRequest request;

                    if (sandbox == true)
                    {
                        request = WebRequest.Create("https://sandbox.itunes.apple.com/verifyReceipt") as HttpWebRequest;
                    }
                    else
                    {
                        request = WebRequest.Create("https://buy.itunes.apple.com/verifyReceipt") as HttpWebRequest;
                    }


                    request.Method = "POST";
                    request.ContentType = "text/plain";
                    request.ContentLength = postBytes.Length;
                    using (Stream postStream = request.GetRequestStream())
                    {
                        await postStream.WriteAsync(postBytes, 0, postBytes.Length);
                        postStream.Close();
                    }


                    Result result = null;

                    using (WebResponse r = await request.GetResponseAsync())
                    {
                        using (System.IO.StreamReader sr = new System.IO.StreamReader(r.GetResponseStream()))
                        {
                            var data = sr.ReadToEnd();
                            result = Newtonsoft.Json.JsonConvert.DeserializeObject<Caspar.Apple.Receipt.Result>(data);
                        }
                    }

                    if (result.status == 21007 && sandbox == false)
                    {
                        return await Verify(receiptData, true);
                    }
                    return result;
                }
                catch (Exception ex)
                {
                    global::Caspar.Api.Logger.Debug(ex);
                    // We crashed and burned — do something intelligent
                }
            }

            return new Result() { status = 1 };

        }
    }
}
