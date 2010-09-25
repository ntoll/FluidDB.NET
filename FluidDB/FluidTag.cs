using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;

namespace FluidDB
{
    /// <summary>
    /// A FluidDB user, for example named Sara, can tag as many different objects 
    /// as she likes with her tags, using whatever values she likes. For example, 
    /// she might tag an object representing The Eiffel Tower, with a sara/opinion 
    /// of beautiful and another object representing Quantum Electrodynamics, with 
    /// the sara/opinion of hard.
    /// </summary>
    public class FluidTag
    {
        private FluidConnector Connection { get; set; }

        public string Name { get; private set; }
        public string Description { get; set; }
        public FluidDBGUID ID { get; private set; }
        public bool Indexed { get; private set; }

        internal FluidTag(FluidConnector c, string name)
        {
            Connection = c;
            Name = name;
        }

        /// <summary>
        /// Returns an object reference to the tag with the specified name in the specified namespace
        /// </summary>
        /// <param name="c"></param>
        /// <param name="f_namespace"></param>
        /// <param name="name"></param>
        /// <param name="returnDescription"></param>
        /// <returns></returns>
        public static FluidTag GetTag(FluidConnector c, FluidNamespace f_namespace, string name, bool returnDescription)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters.Add("returnDescription", returnDescription.ToString());

            // send get request with parameters in URI (.net does not allow you to use the payload
            string path = "/tags/" + (f_namespace != null ? f_namespace.Name + "/" : "") + name;
            HttpWebResponse r = c.Call(METHOD.GET, path, parameters, null);

            if (r.StatusCode == HttpStatusCode.OK)
            {
                Dictionary<string, object> response = c.GetJsonResultDictionary(r);

                FluidTag t = new FluidTag(c, (f_namespace != null ? f_namespace.Name + "/" : "") + name);
                if (returnDescription)
                {
                    t.Description = response["description"].ToString();
                }
                t.ID = new FluidDBGUID(response["id"].ToString());
                t.Indexed = bool.Parse(response["indexed"].ToString());
                return t;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Creates the tag in the namespace
        /// </summary>
        public static FluidTag CreateTag(FluidConnector c, FluidNamespace f_namespace, string name, string description, bool indexed)
        {
            Dictionary<string, object> parameters = new Dictionary<string,object>();
            parameters.Add("description", description);
            parameters.Add("indexed", indexed);
            parameters.Add("name", name);

            HttpWebResponse r = c.Call(METHOD.POST, "/tags/" + f_namespace.Name, null, parameters);
            if (r.StatusCode == HttpStatusCode.Created)
            {
                Dictionary<string, object> d = c.GetJsonResultDictionary(r);

                FluidTag t = new FluidTag(c, f_namespace + "/" + name);
                t.Description = description;
                t.Indexed = indexed;
                t.ID = new FluidDBGUID(d["id"].ToString());

                return t;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Returns the value of the tag as a string
        /// </summary>
        /// <param name="objectID"></param>
        /// <returns></returns>
        public string GetValue(FluidDBGUID objectID)
        {
            // send get request with parameters in URI (.net does not allow you to use the payload
            HttpWebResponse r = Connection.Call(METHOD.GET, "/objects/" + objectID.ID + "/" + Name, null, FluidConnector.PrimitivePutType, "");

            if (r.StatusCode == HttpStatusCode.OK)
            {
                string value = Connection.GetRawResult(r);
                // remove quotes
                return value.Remove(0,1).Remove(value.Length - 2,1);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Returns the tag value as a string, must use this method if value is stored with a custom content type
        /// </summary>
        /// <param name="objectID"></param>
        /// <param name="contentType"></param>
        /// <returns></returns>
        public string GetValue(FluidDBGUID objectID, string contentType)
        {
            // send get request with parameters in URI (.net does not allow you to use the payload
            HttpWebResponse r = Connection.Call(METHOD.GET, "/objects/" + objectID.ID + "/" + Name, null, contentType, "");

            if (r.StatusCode == HttpStatusCode.OK)
            {
                string value = Connection.GetRawResult(r);
                return value;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Loads the ID, description and index of the tag
        /// (gets the tags of the fluiddb/tag object)
        /// </summary>
        public void GetTagInformation()
        {
            FluidTag t = FluidTag.GetTag(Connection, null, Name, true);
            ID = t.ID;
            Indexed = t.Indexed;
            Description = t.Description;
        }
    }
}
