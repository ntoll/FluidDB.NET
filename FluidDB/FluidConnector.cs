using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Web;
using System.IO;
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
        public const string URL = "http://fluiddb.fluidinfo.com/";

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
            return this.Call(m, path, string.Empty);
        }

        /// <summary>
        /// Makes a call to FluidDB
        /// </summary>
        /// <param name="method">The type of HTTP method</param>
        /// <param name="path">The path to call</param>
        /// <param name="body">The body of the request</param>
        /// <returns>The raw result</returns>
        public HttpWebResponse Call(METHOD m, string path, string body)
        {
            return this.Call(m, path, body, new Dictionary<string, string>());
        }

        /// <summary>
        /// Makes a call to FluidDB
        /// </summary>
        /// <param name="method">The type of HTTP method</param>
        /// <param name="path">The path to call</param>
        /// <param name="body">The body of the request</param>
        /// <param name="args">Any further arguments to append to the URI</param>
        /// <returns></returns>
        public HttpWebResponse Call(METHOD m, string path, string body, Dictionary<string, string> args)
        {
            // Process the URI
            StringBuilder URI = new StringBuilder();
            URI.Append(URL);
            URI.Append(path);
            if (this.alwaysUseJson)
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
            Uri requestUri = new Uri(URI.ToString(), true);
            WebRequest request = WebRequest.Create(requestUri);
            switch (m)
            {
                case METHOD.POST:
                    request.Method = "POST";
                    break;
                case METHOD.GET:
                    request.Method = "GET";
                    break;
                case METHOD.PUT:
                    request.Method = "PUT";
                    break;
                case METHOD.DELETE:
                    request.Method = "DELETE";
                    break;
            }

            //  Make sure the headers are correct
            ((HttpWebRequest)request).UserAgent = ".NET FluidDB Client";
            ((HttpWebRequest)request).Accept = "application/json";
            if (!(this.password == string.Empty & this.username == string.Empty))
            {
                request.Credentials = new NetworkCredential(this.username, this.password);
            }
            if (body == string.Empty)
            {
                request.ContentType = "text/plain";
            }
            else
            {
                ASCIIEncoding e = new ASCIIEncoding();
                Byte[] byteArray = e.GetBytes(body);
                request.ContentType = "application/json";
                request.ContentLength = byteArray.Length;
                Stream bodyStream = request.GetRequestStream();
                bodyStream.Write(byteArray, 0, byteArray.Length);
                bodyStream.Close();
            }
            // Call FluidDB
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            return response;
        }

        /// <summary>
        /// Utility method: given a WebResponse object this method will return the 
        /// "result" from FluidDB contained therein.
        /// </summary>
        /// <param name="response">The response from FluidDB</param>
        /// <returns>The "result" from FluidDB</returns>
        public string GetResult(WebResponse response)
        {
            Stream answer = response.GetResponseStream();
            StreamReader sr = new StreamReader(answer);
            return sr.ReadToEnd();
        }

        /// <summary>
        /// In the absence of bolting on nUnit and in order to keep the dependencies to a
        /// minimum I'm testing the class with the following method. Pass in your username
        /// and password in order to be able to post
        /// </summary>
        /// <param name="username">Your username</param>
        /// <param name="password">Your password</param>
        public void SelfTest(string username, string password)
        {
            // Return a list of objects that have the tag “username” from the “fluiddb/users” namespace
            Dictionary<string, string> args = new Dictionary<string, string>();
            args.Add("query", "has fluiddb/users/username");
            HttpWebResponse result = this.Call(METHOD.GET, "objects", "", args);
            Debug.Assert((HttpStatusCode.OK == result.StatusCode), "Didn't return 200");
            Debug.Assert((this.GetResult(result).Length > 0), "Didn't return anything!");

            // Return an object where the tag “username” from the “fluiddb/users” namespace has the value “ntoll”
            args["query"] = "fluiddb/users/username = \"ntoll\"";
            result = this.Call(METHOD.GET, "objects", "", args);
            Debug.Assert((HttpStatusCode.OK == result.StatusCode), "Didn't return 200");
            Debug.Assert((this.GetResult(result).Contains("'ids':")), "Didn't return anything valid!");

            // Find out about a specific object
            result = this.Call(METHOD.GET, "/objects/5873e7cc-2a4a-44f7-a00e-7cebf92a7332", "{'showAbout': True}");
            Debug.Assert((HttpStatusCode.OK == result.StatusCode), "Didn't return 200");

            // Get the value of the tag “fluiddb/users/username” from the object with the uuid “5873e7cc-2a4a-44f7-a00e-7cebf92a7332”
            result = this.Call(METHOD.GET, "/objects/5873e7cc-2a4a-44f7-a00e-7cebf92a7332/fluiddb/users/name");
            Debug.Assert((HttpStatusCode.OK == result.StatusCode), "Didn't return 200");
            Debug.Assert((this.GetResult(result) == "ntoll"), "Didn't return expected result!");

            // Get the same result as json
            this.AlwaysUseJson = true;
            result = this.Call(METHOD.GET, "/objects/5873e7cc-2a4a-44f7-a00e-7cebf92a7332/fluiddb/users/name");
            Debug.Assert((HttpStatusCode.OK == result.StatusCode), "Didn't return 200");
            Debug.Assert((this.GetResult(result) == "{'value': 'ntoll'}"), "Didn't return expected result!");
        }
    }
}