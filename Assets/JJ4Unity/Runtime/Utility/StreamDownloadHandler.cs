using System.IO;
using JJ4Unity.Runtime.Extension;
using UnityEngine.Networking;

namespace JJ4Unity.Runtime.Utility
{
    public class StreamDownloadHandler : DownloadHandlerScript
    {
        private readonly MemoryStream _stream = new();

        protected override bool ReceiveData(byte[] receivedData, int dataLength)
        {
            if (null == receivedData || 0 == receivedData.Length)
            {
                Debug.LogWarning("Receive data is null or empty.");
                return false;
            }

            _stream.Write(receivedData, 0, dataLength);
            return true;
        }

        protected override void CompleteContent()
        {
            _stream.Seek(0, SeekOrigin.Begin);
        }

        protected override byte[] GetData()
        {
            return _stream.ToArray();
        }

        public Stream GetStream()
        {
            _stream.Seek(0, SeekOrigin.Begin);
            return _stream;
        }

        public override void Dispose()
        {
            base.Dispose();
            _stream.Dispose();
        }
    }
}
