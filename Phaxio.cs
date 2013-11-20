using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Web.Script.Serialization;

//
//  2012-??-??  xyzzy / Phaxio, Inc.
//              Created
//
//  2013-08-30  Mark Stega / Optimium Health, Inc.
//              Changed class name so there is no conflict between the namespace and class. Otherwise you'd have to qualify everything with "Phaxio."
//              Added private ctor with no arguments
//              Made ctor with three arguments, just intialize the hostURL directly
//              Changed all method returns to be of type PhaxioOperationResult
//              Removed non-value-add "sendData"
//              Changed copyParms to copyValidOptionsToParameters
//              Added to validOptionNames in sendFax
//              Change name createAndSendRequest, cleaned up try/catch handling
//              Removed PhaxioException
//              Changed PhaxioOperationResult to use simple properties
//

namespace Phaxio
{
    public class PhaxioAPI
    {
        private bool debug = false;
        private string api_key;
        private string api_secret;
        private string host;

        private PhaxioAPI() { }

        public PhaxioAPI(string apiKey, string apiSecret, string hostURL = "https://api.phaxio.com/v1/")
        {
            api_key = apiKey;
            api_secret = apiSecret;
            host = hostURL;
        }

        public PhaxioOperationResult faxStatus(int faxId)
        {
            if (faxId == 0)
                return new PhaxioOperationResult(false, "A valid fax id is required.");
            NameValueCollection parameters = new NameValueCollection();
            parameters.Add("id", faxId.ToString());
            return doRequest(host + "faxStatus", parameters);
        }

        public PhaxioOperationResult sendFax(string[] to, string[] filenames, Dictionary<string, string> options)
        {
            if ((to == null) || (to.Length == 0))
                return new PhaxioOperationResult(false, "You must include a 'to' number.");

            if (filenames == null && !options.ContainsKey("string_data"))
                return new PhaxioOperationResult(false, "You must include a file.");

            NameValueCollection parameters = new NameValueCollection();
            
            for (int i = 0; i < to.Length; i++)
            {
                parameters.Add(System.String.Format("to[{0}]", i), to[i]);
            }
            if (filenames != null)
                for (int i = 0; i < filenames.Length; i++)
                {
                    if (!File.Exists(@filenames[i]))
                    {
                        return new PhaxioOperationResult(false, System.String.Format("The file '{0}' does not exist.", filenames[i]));
                    }
                    parameters.Add(System.String.Format("filename[{0}]", i), filenames[i]);
                }
            string[] validOptionNames = new string[]{
                "string_data",
                "string_data_type",
                "batch",
                "batch_delay",
                "batch_collision!avoidance",
                "callback_url",
                "cancel_timeout",
                "caller_id"};
            copyValidOptionsToParameters(validOptionNames, options, parameters);
            return doRequest(host + "send", parameters);
        }

        //public PhaxioOperationResult fireBatch(int batchId){
        //    if(batchId==0)
        //        return new PhaxioOperationResult(false, "You need to include a batch Id.");
        //    NameValueCollection parameters = new NameValueCollection();
        //    parameters.Add("id",batchId.ToString());
        //    return doRequest(host+"fireBatch",parameters);
        //}

        //public PhaxioOperationResult closeBatch(int batchId){
        //    if(batchId==0)
        //        return new PhaxioOperationResult(false, "You need to include a batch Id.");
        //    NameValueCollection parameters = new NameValueCollection();
        //    parameters.Add("id",batchId.ToString());
        //    return doRequest(host+"closeBatch",parameters);
        //}

        public string getApiKey()
        {
            return api_key;
        }

        public string getApiSecret()
        {
            return api_secret;
        }

        public PhaxioOperationResult provisionNumber(int areaCode, string callbackURL = "")
        {
            if (areaCode == 0)
                return new PhaxioOperationResult(false, "Area Code is required.");
            NameValueCollection parameters = new NameValueCollection();
            parameters.Add("area_code", areaCode.ToString());
            if (callbackURL != "")
            {
                parameters.Add("callback_url", callbackURL);
            }
            return doRequest(host + "provisionNumber", parameters);
        }

        public PhaxioOperationResult releaseNumber(string number)
        {
            if (number == "")
            {
                return new PhaxioOperationResult(false, "A fax number is required.");
            }
            NameValueCollection parameters = new NameValueCollection();
            parameters.Add("number", number);
            return doRequest(host + "releaseNumber", parameters);
        }

        public PhaxioOperationResult numberList(Dictionary<string, string> options = null)
        {
            NameValueCollection parameters = new NameValueCollection();
            if (options != null)
            {
                copyValidOptionsToParameters(new string[] { "area_code", "number" }, options, parameters);
            }
            return doRequest(host + "numberList", parameters);
        }

        public PhaxioOperationResult accountStatus()
        {
            NameValueCollection parameters = new NameValueCollection();
            return doRequest(host + "accountStatus", parameters);
        }

        public PhaxioOperationResult testReceive(string filename, Dictionary<string, string> options = null)
        {
            if (filename == null || filename == "" || !File.Exists(@filename) || Path.GetExtension(filename) != ".pdf")
            {
                return new PhaxioOperationResult(false, "You must specify a valid pdf file.");
            }
            NameValueCollection parameters = new NameValueCollection();
            parameters.Add("filename", filename);
            if (options != null)
            {
                copyValidOptionsToParameters(new string[] { "from_number", "to_number" }, options, parameters);
            }
            return doRequest(host + "testReceive", parameters);
        }

        public PhaxioOperationResult attachPhaxCode(float x, float y, string filename, Dictionary<string, string> options = null)
        {
            if (filename == null || filename == "" || !File.Exists(@filename) || Path.GetExtension(filename) != ".pdf")
            {
                return new PhaxioOperationResult(false, "You must specify a valid pdf file.");
            }
            NameValueCollection parameters = new NameValueCollection();
            parameters.Add("filename", filename);
            parameters.Add("x", x.ToString());
            parameters.Add("y", y.ToString());
            if (options != null)
            {
                copyValidOptionsToParameters(new string[] { "metadata", "page_number" }, options, parameters);
            }
            return doRequest(host + "attachPhaxCodeToPdf", parameters);
        }

        public PhaxioOperationResult createPhaxCode(Dictionary<string, string> options = null)
        {
            NameValueCollection parameters = new NameValueCollection();
            if (options != null)
            {
                copyValidOptionsToParameters(new string[] { "metadata", "redirect" }, options, parameters);
            }
            return doRequest(host + "createPhaxCode", parameters);
        }

        public PhaxioOperationResult getHostedDocument(string name, string metadata = null)
        {
            if (name == null || name == "")
            {
                return new PhaxioOperationResult(false, "You must include a document name.");
            }
            NameValueCollection parameters = new NameValueCollection();
            if (metadata != null || metadata != "")
            {
                parameters.Add("metadata", metadata);
            }
            return doRequest(host + "getHostedDocument", parameters);
        }

        public PhaxioOperationResult faxFile(int id, string type = "p")
        {
            if (id == 0)
            {
                return new PhaxioOperationResult(false, "A fax id is required.");
            }
            NameValueCollection parameters = new NameValueCollection();
            parameters.Add("id", id.ToString());
            parameters.Add("type", type);
            return doRequest(host + "faxFile", parameters);
        }

        public PhaxioOperationResult faxList(string start, string end, Dictionary<string, string> options = null)
        {
            if (start == null || start == "" || end == "" || end == null)
                return new PhaxioOperationResult(false, "Start and end timestamps are required.");
            NameValueCollection parameters = new NameValueCollection();
            parameters.Add("start", start);
            parameters.Add("end", end);
            if (options != null)
            {
                copyValidOptionsToParameters(new string[] { "page", "max_per_page" }, options, parameters);
            }
            return doRequest(host + "faxList", parameters);
        }

        private PhaxioOperationResult doRequest(string address, NameValueCollection parameters)
        {
            parameters.Add("api_key", api_key);
            parameters.Add("api_secret", api_secret);
            if (debug)
                Console.WriteLine("Request address: \n\n {0}?{1}", address, ToString(parameters));
            var response = createAndSendRequest(address, parameters);
            if (!response.Success)
            {
                if (debug)
                    Console.WriteLine("Failed in createAndSendRequest: \n\n{0}\n\n", response.Message);

                return response;
            }

            if (debug)
                Console.WriteLine("Response message: \n\n{0}\n\n", response.Message);

            JavaScriptSerializer responsed = new JavaScriptSerializer();
            dynamic res;
            try
            {
                res = responsed.Deserialize<dynamic>(response.Message);
            }
            catch
            {
                return new PhaxioOperationResult(false, "No data received from service.");
            }
            string message = null;
            Dictionary<string, object> data = null;
            bool success = false;
            if (res.ContainsKey("success"))
                success = res["success"];
            if (res.ContainsKey("message"))
                message = res["message"];
            if (res.ContainsKey("data"))
                data = res["data"];
            return new PhaxioOperationResult(success, message, data);
        }

        private string ToString(NameValueCollection source)
        {
            var str = new StringBuilder();
            foreach (KeyValuePair<string, string> kv in source)
                str.Append(string.Format("{0}{1}{2}{3}", kv.Key, '=', kv.Value, '&'));
            var retval = str.ToString();
            return retval.Substring(0, retval.Length - 1);
        }

        private void copyValidOptionsToParameters(string[] validParameterNames, Dictionary<string, string> options, NameValueCollection parameters)
        {
            foreach (string name in validParameterNames)
            {
                if (options.ContainsKey(name))
                {
                    parameters.Add(name, options[name]);
                }
            }
        }

        private PhaxioOperationResult createAndSendRequest(string url, NameValueCollection parameters)
        {
            string boundary = "---------------------------" + getSHA(DateTime.Now.Ticks.ToString("x"));
            byte[] boundarybytes = System.Text.Encoding.ASCII.GetBytes("\r\n--" + boundary + "\r\n");

            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(url);
            webRequest.ContentType = "multipart/form-data; boundary=" + boundary;
            webRequest.Method = "POST";
            webRequest.KeepAlive = true;
            webRequest.Credentials = System.Net.CredentialCache.DefaultCredentials;

            Stream requestStream = webRequest.GetRequestStream();

            string formdataTemplate = "Content-Disposition: form-data; name=\"{0}\"\r\n\r\n{1}";
            string headerTemplate = "Content-Disposition: form-data; name=\"{0}\"; filename=\"{1}\"\r\nContent-Type: application/octet-stream\r\n\r\n";
            foreach (string key in parameters.Keys)
            {
                requestStream.Write(boundarybytes, 0, boundarybytes.Length);
                if (key.StartsWith("filename"))
                {
                    string header = string.Format(headerTemplate, key, parameters[key]);
                    byte[] headerbytes = System.Text.Encoding.UTF8.GetBytes(header);
                    requestStream.Write(headerbytes, 0, headerbytes.Length);
                    FileStream fileStream = new FileStream(parameters[key], FileMode.Open, FileAccess.Read);
                    byte[] buffer = new byte[4096];
                    int bytesRead = 0;
                    while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) != 0)
                    {
                        requestStream.Write(buffer, 0, bytesRead);
                    }
                    fileStream.Close();
                }
                else
                {

                    string formitem = string.Format(formdataTemplate, key, parameters[key]);
                    byte[] formitembytes = System.Text.Encoding.UTF8.GetBytes(formitem);
                    requestStream.Write(formitembytes, 0, formitembytes.Length);
                }
            }
            byte[] trailer = System.Text.Encoding.ASCII.GetBytes("\r\n--" + boundary + "--\r\n");
            requestStream.Write(trailer, 0, trailer.Length);
            requestStream.Close();

            WebResponse webResponse = null;
            try
            {
                webResponse = webRequest.GetResponse();
                Stream stream2 = webResponse.GetResponseStream();
                StreamReader reader2 = new StreamReader(stream2);
                string message = reader2.ReadToEnd();
                webResponse.Close();
                webResponse = null;
                webRequest = null;
                return new PhaxioOperationResult(true, message);
            }
            catch(Exception ex)
            {
                if (webResponse != null)
                {
                    webResponse.Close();
                    webResponse = null;
                }
                webRequest = null;
                return new PhaxioOperationResult(false, ex.ToString());
            }
        }

        private string getSHA(string data)
        {
            SHA1 temp = new SHA1CryptoServiceProvider();
            UnicodeEncoding UE = new UnicodeEncoding();
            byte[] hash = temp.ComputeHash(UE.GetBytes(data));
            string result = "";
            foreach (var b in hash)
                result += b.ToString("X2");
            return result;
        }

    }


    public class PhaxioOperationResult
    {
        public PhaxioOperationResult(bool success, string message, Dictionary<string, object> data = null)
        {
            Success = success;
            Message = message;
            Data = data;
        }

        public bool Success { get; set; }

        public string Message { get; set; }

        public Dictionary<string, object> Data { get; set; }
    }

}