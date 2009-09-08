using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.IO;
using System.Web.Script.Serialization;
using NUnit.Framework;
using FluidDB;

namespace UnitTests
{
    /// <summary>
    /// Tests the FluidConnector class
    /// </summary>
    [TestFixture()]
    public class TestFluidConnector
    {

        /// <summary>
        /// Instant to use when testing, SetUpFixture method will make sure it's
        /// clean for each test
        /// </summary>
        private FluidConnector fdb;

        /// <summary>
        /// To hold where the user's credentials are to be stored for authentication
        /// 
        /// To get credentials for FluidDB and the Sandbox please visit:
        /// 
        /// http://fluidinfo.com/accounts/new
        /// </summary>
        public static string CREDENTIALS
        {
            get
            {
                return Path.Combine(System.IO.Path.GetTempPath(), "credentials.json");
            }
        }

        private string username = string.Empty;
        private string password = string.Empty;

        private string object_id = string.Empty;

        /// <summary>
        /// The UUID for a known existing object in the FluidDB (against which we
        /// </summary>
        public string ObjectID
        {
            get { return this.object_id; }
        }

        /// <summary>
        /// One time setup for the run of the unit tests
        /// </summary>
        [TestFixtureSetUp]
        public void FixtureSetup()
        {
            // lets make sure we definitely have an object with a known id in the sandbox database
            this.fdb = new FluidConnector();
            this.fdb.URL = FluidConnector.SANDBOX;

            // lets check for a credentials.json file in the file specified by CREDENTIALS constant
            FileInfo fi = new FileInfo(CREDENTIALS);
            if (fi.Exists)
            {
                JavaScriptSerializer jss = new JavaScriptSerializer();
                StreamReader sr = fi.OpenText();
                Dictionary<string, string> credentials = jss.Deserialize<Dictionary<string, string>>(sr.ReadToEnd());
                this.username = credentials["username"];
                this.password = credentials["password"];
            }
            else
            {
                throw new Exception("Please include your username and password in the following file: " + CREDENTIALS);
            }

            // Finally lets make sure we have an object we know exists for the purpose of testing
            Dictionary<string, object> about = new Dictionary<string, object>();
            about.Add("about", "Created for the purpose of unit-testing the client .NET library");
            HttpWebResponse response = this.fdb.Call(METHOD.POST, "/objects", about);
            Dictionary<string, object> dict = this.fdb.GetJsonResultDictionary(response);
            this.object_id = (string)dict["id"];
        }

        /// <summary>
        /// SetUpFixture method - per test setup goes here
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            this.fdb = new FluidConnector();
            this.fdb.URL = FluidConnector.SANDBOX;
            this.fdb.Username = this.username;
            this.fdb.Password = this.password;
        }

        /// <summary>
        /// Test an HTTP GET call
        /// </summary>
        [Test]
        public void TestCallGet()
        {
            // Call with the query language
            Dictionary<string, string> args = new Dictionary<string, string>();
            args.Add("query", "has fluiddb/users/username");
            HttpWebResponse result = this.fdb.Call(METHOD.GET, "/objects", args);
            Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
            Assert.Greater(this.fdb.GetRawResult(result).Length, 0);
            // Call with no other args
            // Get the value of the tag “fluiddb/about” from the object with the uuid “5873e7cc-2a4a-44f7-a00e-7cebf92a7332”
            result = this.fdb.Call(METHOD.GET, "/objects/"+this.ObjectID+"/fluiddb/about");
            Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
            Assert.AreEqual("Created for the purpose of unit-testing the client .NET library", this.fdb.GetRawResult(result));
        }

        /// <summary>
        /// Test an HTTP POST call
        /// </summary>
        [Test]
        public void TestCallPost()
        {
            // Lets post to namespaces to create a new one
            string newNamespaceName = System.Guid.NewGuid().ToString("N");
            Dictionary<string, object> jsonPayload = new Dictionary<string, object>();
            jsonPayload.Add("description", "Created for the purpose of unit-testing the client .NET library");
            jsonPayload.Add("name", newNamespaceName);
            HttpWebResponse response = this.fdb.Call(METHOD.POST, "/namespaces/"+this.fdb.Username, jsonPayload);
            Dictionary<string, object> dict = this.fdb.GetJsonResultDictionary(response);
            Assert.AreEqual(HttpStatusCode.Created, response.StatusCode, this.fdb.GetRawResult(response));
            Assert.AreEqual(true, dict.ContainsKey("id"));
            Assert.AreEqual(true, dict.ContainsKey("URI"));
        }

        /// <summary>
        /// Test an HTTP PUT call
        /// </summary>
        [Test]
        public void TestCallPut()
        {
            // Create a new namespace for the purposes of testing...
            string newNamespaceName = System.Guid.NewGuid().ToString("N");
            Dictionary<string, object> jsonPayload = new Dictionary<string, object>();
            jsonPayload.Add("description", "Created for the purpose of unit-testing the client .NET library");
            jsonPayload.Add("name", newNamespaceName);
            HttpWebResponse response = this.fdb.Call(METHOD.POST, "/namespaces/" + this.fdb.Username, jsonPayload);
            Dictionary<string, object> dict = this.fdb.GetJsonResultDictionary(response);
            Assert.AreEqual(HttpStatusCode.Created, response.StatusCode, this.fdb.GetRawResult(response));
            Assert.AreEqual(true, dict.ContainsKey("id"));
            Assert.AreEqual(true, dict.ContainsKey("URI"));
            // Lets update the description with a PUT
            jsonPayload = new Dictionary<string, object>();
            jsonPayload.Add("description", "updated");
            response = this.fdb.Call(METHOD.PUT, "/namespaces/" + this.fdb.Username + "/" + newNamespaceName, jsonPayload);
            Assert.AreEqual(HttpStatusCode.NoContent, response.StatusCode);
        }

        /// <summary>
        /// Test an HTTP HEAD call
        /// </summary>
        [Test]
        public void TestCallHead()
        {
            // The HEAD method on objects can be used to test whether an object has a given 
            // tag or not, without retrieving the value of the tag. 

            // This object *does* have the tag
            HttpWebResponse result = this.fdb.Call(METHOD.HEAD, "/objects/"+this.ObjectID+"/fluiddb/about");
            Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);

            // This object *doesn't* have the tag (404 will also be returned if the
            // user doesn't have SEE permission for the tag
            result = this.fdb.Call(METHOD.HEAD, "/objects/" + this.ObjectID + "/fluiddb/users/username");
            Assert.AreEqual(HttpStatusCode.NotFound, result.StatusCode);
        }

        /// <summary>
        /// Test an HTTP Delete call
        /// </summary>
        [Test]
        public void TestCallDelete()
        {
            // Create a new namespace for the purposes of testing...
            string newNamespaceName = System.Guid.NewGuid().ToString("N");
            Dictionary<string, object> jsonPayload = new Dictionary<string, object>();
            jsonPayload.Add("description", "Created for the purpose of unit-testing the client .NET library");
            jsonPayload.Add("name", newNamespaceName);
            HttpWebResponse response = this.fdb.Call(METHOD.POST, "/namespaces/" + this.fdb.Username, jsonPayload);
            Dictionary<string, object> dict = this.fdb.GetJsonResultDictionary(response);
            Assert.AreEqual(HttpStatusCode.Created, response.StatusCode, this.fdb.GetRawResult(response));
            Assert.AreEqual(true, dict.ContainsKey("id"));
            Assert.AreEqual(true, dict.ContainsKey("URI"));
            // Lets delete the namespace with a DELETE
            response = this.fdb.Call(METHOD.DELETE, "/namespaces/" + this.fdb.Username + "/" + newNamespaceName);
            Assert.AreEqual(HttpStatusCode.NoContent, response.StatusCode);
        }

        /// <summary>
        /// Test the various user credential call options
        /// </summary>
        [Test]
        public void TestUserCredentials()
        {
            // Check we can do something that requires authorization because *you* have 
            // supplied appropriate credentials
            string newNamespaceName = System.Guid.NewGuid().ToString("N");
            Dictionary<string, object> jsonPayload = new Dictionary<string, object>();
            jsonPayload.Add("description", "Created for the purpose of unit-testing the client .NET library");
            jsonPayload.Add("name", newNamespaceName);
            HttpWebResponse response = this.fdb.Call(METHOD.POST, "/namespaces/" + this.fdb.Username, jsonPayload);
            Dictionary<string, object> dict = this.fdb.GetJsonResultDictionary(response);
            Assert.AreEqual(HttpStatusCode.Created, response.StatusCode, this.fdb.GetRawResult(response));
            Assert.AreEqual(true, dict.ContainsKey("id"));
            Assert.AreEqual(true, dict.ContainsKey("URI"));

            // Now lets try it with wrong credentials
            this.fdb.Password = "not_a_valid_password";
            newNamespaceName = System.Guid.NewGuid().ToString("N");
            response = this.fdb.Call(METHOD.POST, "/namespaces/" + this.fdb.Username, jsonPayload);
            Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);

            // Lets try something that doesn't require credentials
            this.fdb.Username = string.Empty;
            this.fdb.Password = string.Empty;
            Dictionary<string, string> args = new Dictionary<string, string>();
            args.Add("query", "has fluiddb/users/username");
            response = this.fdb.Call(METHOD.GET, "/objects", args);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.Greater(this.fdb.GetRawResult(response).Length, 0);
        }

        /// <summary>
        /// For GET and PUT, make sure we get back json (if specified)
        /// </summary>
        [Test]
        public void TestAlwaysUseJson()
        {
            // GET (without json)
            Assert.AreEqual(false, this.fdb.AlwaysUseJson);
            // Get the value of the tag “fluiddb/about” from the object with the uuid “5873e7cc-2a4a-44f7-a00e-7cebf92a7332”
            HttpWebResponse result = fdb.Call(METHOD.GET, "/objects/"+this.ObjectID+"/fluiddb/about");
            Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
            Assert.AreEqual("text/plain; charset=UTF-8", result.ContentType);
            Assert.AreEqual("Created for the purpose of unit-testing the client .NET library", fdb.GetRawResult(result));
            // GET (with json)
            this.fdb.AlwaysUseJson = true;
            result = fdb.Call(METHOD.GET, "/objects/"+this.ObjectID+"/fluiddb/about");
            Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
            Assert.AreEqual("application/json", result.ContentType);
            Assert.AreEqual("{\"value\": \"Created for the purpose of unit-testing the client .NET library\"}", fdb.GetRawResult(result));
        }

        /// <summary>
        /// Make sure this helper method works properly in various situations
        /// </summary>
        [Test]
        public void TestGetRawResult()
        {
            HttpWebResponse result = fdb.Call(METHOD.GET, "/objects/" + this.ObjectID + "/fluiddb/about");
            Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
            Assert.AreEqual("Created for the purpose of unit-testing the client .NET library", fdb.GetRawResult(result));
        }

        /// <summary>
        /// Makes sure we get an appropriate dictionary from a json payload in a result
        /// </summary>
        [Test]
        public void TestGetJsonResultDictionary()
        {
            this.fdb.AlwaysUseJson = true;
            HttpWebResponse result = fdb.Call(METHOD.GET, "/objects/" + this.ObjectID + "/fluiddb/about");
            Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
            Dictionary<string, object> dict = fdb.GetJsonResultDictionary(result);
            Assert.AreEqual(1, dict.Count);
            Assert.AreEqual("Created for the purpose of unit-testing the client .NET library", dict["value"]);
        }
    }
}
