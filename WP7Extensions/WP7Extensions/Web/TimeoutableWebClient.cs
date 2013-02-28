using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Phone.Reactive;

namespace WP7Extensions.Web
{
    public class TimeoutableWebClient
    {
        private int _status = 0;

        public string Url { get; set; }
        public string Method { get; set; }
        public string ContentType { get; set; }

        public HttpWebRequest Request { get; set; }

        public TimeoutableWebClient(string url) {
            this.Url = url;
            Method = "GET";
            Request = (HttpWebRequest)WebRequest.Create(Url);
        }

        public void DownloadStringAsync(TimeSpan timeout) {
            _status = 1;

            if (!String.IsNullOrEmpty(ContentType)) Request.ContentType = ContentType;
            if (!String.IsNullOrEmpty(Method)) Request.Method = Method;

            var o = Observable.FromAsyncPattern<WebResponse>(Request.BeginGetResponse, Request.EndGetResponse)()
                .Timeout(timeout)
                .ObserveOnDispatcher();

            var s = o.Subscribe(onNext: OnNext, onError: OnError);

        }

        public delegate void DownloadStringCompletedEventHandler(object source, string result);
        public event DownloadStringCompletedEventHandler DownloadStringCompleted;

        private void OnNext(WebResponse result) {
            if (_status == 1) {
                _status = 0;
                try {
                    var response = result.GetResponseStream();
                    var reader = new StreamReader(response);
                    var resultString = reader.ReadToEnd();
                    response.Close();
                    DownloadStringCompleted(this, resultString);
                } catch {
                    DownloadStringCompleted(this, null);
                }
            }
        }

        private void OnError(Exception e) {
            if (_status == 1) {
                _status = 0;
                DownloadStringCompleted(this, null);
            }
        }

        public async Task<string> DownloadStringTaskAsync(TimeSpan timeout)
        {
            if (!String.IsNullOrEmpty(ContentType)) Request.ContentType = ContentType;
            if (!String.IsNullOrEmpty(Method)) Request.Method = Method;
            try
            {
                var task = Request.GetResponseAsync();
                var status = Task.WaitAny(new Task[] {task}, timeout);

                if (status != 0 || task.Result == null) {
                    throw new Exception("Cannot download from current Url");                    
                }

                var response = task.Result.GetResponseStream();
                var reader = new StreamReader(response);
                var resultString = reader.ReadToEnd();
                response.Close();
                return resultString;
            }
            catch (Exception)
            {
                throw new Exception("Cannot download from current Url");
            }
        }
    }
}
