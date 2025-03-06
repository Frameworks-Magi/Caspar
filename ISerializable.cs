using System;
namespace Caspar
{
    public interface ISerializable
    {
        void Serialize(System.IO.Stream output);
        int Length { get; }
    }
}
