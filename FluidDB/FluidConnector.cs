using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Web;
using System.IO;
using System.Web.Script.Serialization;
using System.Diagnostics;

namespace FluidDB
{
    /// <summary>
    /// Allowed HTTP methods
    /// </summary>
    public enum METHOD
    {
        POST,
        GET,
        HEAD,
        PUT,
        DELETE
    }

    /// <summary>
    /// A base class defining the call methods for all the other FluidDB classes
    /// </summary>
    public class FluidConnector
    {
        /// <summary>
        /// The URL for FluidDB
        /// </summary>
        public const string FLUIDDB = "http://fluiddb.fluidinfo.com";

        /// <summary>
        /// The URL for the Sandbox for development testing
        /// </summary>
        public const string SANDBOX = "http://sandbox.fluidinfo.com";

        /// <summary>
        /// Used for parsing json
        /// </summary>
        private JavaScriptSerializer jss = new JavaScriptSerializer();

        private string url = FluidConnector.FLUIDDB;

        /// <summary>
        /// The URL to use for connecting to FluidDB
        /// </summary>
        public string URL
        {
            get { return this.url; }
            set { this.url = value; }
        }

        /// <summary>
        /// Use format=json for responses from GET and PUT requests
        /// 
        /// (FluidDB defaults to raw payload)
        /// </summary>
        private bool alwaysUseJson = false;

        /// <summary>
        /// Use format=json for responses from GET and PUT requests
        /// 
        /// (FluidDB defaults to raw payload)
        /// </summary>
        public bool AlwaysUseJson
        {
            get { return this.alwaysUseJson; }
            set { this.alwaysUseJson = value; }
        }

        /* Used for authenticatation */
        private string username = string.Empty;

        private string password = string.Empty;

        public string Username
        {
            get { return this.username; }
            set { this.username = value; }
        }

        public string Password
        {
            get { return this.password; }
            set { this.password = value; }
        }

        /// <summary>
        /// Makes a call to FluidDB
        /// </summary>
        /// <param name="method">The type of HTTP method</param>
        /// <param name="path">The path to call</param>
        /// <returns>The raw result</returns>
        public HttpWebResponse Call(METHOD m, string path)
        {
            return this.Call(m, path, new Dictionary<string, object>());
        }

        /// <summary>
        /// Makes a call to FluidDB
        /// </summary>
        /// <param name="method">The type of HTTP method</param>
        /// <param name="path">The path to call</param>
        /// <param name="body">The body of the request</param>
        /// <returns>The raw result</returns>
        public HttpWebResponse Call(METHOD m, string path, Dictionary<string, object> body)
        {
            return this.Call(m, path, body, new Dictionary<string, string>());
        }

        /// <summary>
        /// Makes a call to FluidDB
        /// </summary>
        /// <param name="m">The type of HTTP method</param>
        /// <param name="path">The path to call</param>
        /// <param name="args">Any further arguments to append to the URI</param>
        /// <returns>The raw result</returns>
        public HttpWebResponse Call(METHOD m, string path, Dictionary<string, string> args)
        {
            return this.Call(m, path, new Dictionary<string, object>(), args);
        }

        /// <summary>
        /// Makes a call to FluidDB
        /// </summary>
        /// <param name="method">The type of HTTP method</param>
        /// <param name="path">The path to call</param>
        /// <param name="body">The body of the request</param>
        /// <param name="args">Any further arguments to append to the URI</param>
        /// <returns>The raw result</returns>
        public HttpWebResponse Call(METHOD m, string path, Dictionary<string, object> body, Dictionary<string, string> args)
        {
            // Process the URI
            StringBuilder URI = new StringBuilder();
            URI.Append(this.url);
            URI.Append(path);
            if (this.alwaysUseJson & (m == METHOD.GET || m == METHOD.PUT))
            {
                if (!args.ContainsKey("format"))
                {
                    args.Add("format", "json");
                }
            }
            if (args.Count > 0)
            {
                URI.Append("?");
                List<string> argList = new List<string>();
                foreach (string k in args.Keys)
                {
                    argList.Add(k + "=" + HttpUtility.UrlEncode(args[k]));
                }
                URI.Append(string.Join("&", argList.ToArray()));
            }
            // Build the request
            Uri requestUri = new Uri(URI.ToString());
            WebRequest request = WebRequest.Create(requestUri);
            request.Method = m.ToString();
            //  Make sure the headers are correct
            ((HttpWebRequest)request).UserAgent = ".NET FluidDB Client";
            ((HttpWebRequest)request).Accept = "application/json";
            if (!(this.password == string.Empty & this.username == string.Empty))
            {
                string userpass = username+":"+password;
                byte[] encUserPass = Encoding.UTF8.GetBytes(userpass);
                string auth="Basic "+Convert.ToBase64String(encUserPass).Trim();
                ((HttpWebRequest)request).Headers.Add(HttpRequestHeader.Authorization, auth);
            }
            if (body.Count == 0)
            {
                request.ContentType = "text/plain";
            }
            else
            {
                Byte[] byteArray = Encoding.ASCII.GetBytes(this.jss.Serialize(body));
                request.ContentType = "application/json";
                request.ContentLength = byteArray.Length;
                Stream bodyStream = request.GetRequestStream();
                bodyStream.Write(byteArray, 0, byteArray.Length);
                bodyStream.Close();
            }
            // Call FluidDB
            try
            {
                return (HttpWebResponse)request.GetResponse();
            }
            catch (WebException e)
            {
                // I don't want you to raise an exception dammit... I just want the raw 
                // response and I'll process the errorClass from the content and maybe 
                // raise an exception if appropriate elsewhere
                return (HttpWebResponse)e.Response;
            }
        }

        /// <summary>
        /// Utility method: given a WebResponse object this method will return the 
        /// "result" from FluidDB contained therein.
        /// </summary>
        /// <param name="response">The response from FluidDB</param>
        /// <returns>The raw string "result" from FluidDB</returns>
        public string GetRawResult(HttpWebResponse response)
        {
            Stream answer = response.GetResponseStream();
            StreamReader sr = new StreamReader(answer);
            return sr.ReadToEnd();
        }

        /// <summary>
        /// Given a response with a json payload, will return a dictionary representation
        /// of the json
        /// </summary>
        /// <param name="response">The response from FluidDB</param>
        /// <returns>The </returns>
        public Dictionary<string, object> GetJsonResultDictionary(HttpWebResponse response)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();
            if (response.ContentType == "application/json")
            {
                result = this.jss.Deserialize<Dictionary<string, object>>(this.GetRawResult(response));
            }
            return result;
        }
    }
}