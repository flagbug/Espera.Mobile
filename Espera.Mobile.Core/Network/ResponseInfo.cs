namespace Espera.Mobile.Core.Network
{
    public class ResponseInfo
    {
        public ResponseInfo(int statusCode, string message)
        {
            this.StatusCode = statusCode;
            this.Message = message;
        }

        public string Message { get; private set; }

        public int StatusCode { get; private set; }
    }
}