using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Collections;
using System.Collections.ObjectModel;

namespace FluidDB
{
    /// <summary>
    /// FluidDB is conceptually very simple: it holds a large number of objects, all of 
    /// a single flexible kind, and it provides the means to create, modify, and retrieve 
    /// these objects.
    /// 
    /// A FluidDB object is just a collection of tags, usually with values.
    /// 
    /// As with other tagging systems, tags have names, such as tim/opinion, but unlike most 
    /// tag systems, tags can also and usually do have values, such as "very exciting". For 
    /// now, think of an object as a container for tags.
    /// 
    /// When objects are first created, they are completely empty. Each is assigned a unique 
    /// identifier which can be used to carry out operations on it, such as adding or removing 
    /// tags.
    /// 
    /// There is no limit on the number of tags on an object.
    /// 
    /// Any application or user may create new objects at any time, and use them for any 
    /// purpose.
    /// </summary>
    public class FluidObject
    {
        /// <summary>
        /// The connection associated with this object (where it exists)
        /// </summary>
        public FluidConnector Connection { get; private set; }

        /// <summary>
        /// The ID of this object in the instance of FluidDB referenced in Connection
        /// This is readonly
        /// </summary>
        public FluidDBGUID ID { get; private set; }

        /// <summary>
        /// A read only, bindable set of Tags associated with the object
        /// GB: might be preferable to make this IEnumerable so it compiles for .Net 2.0 then no databinding though :(
        /// </summary>
        public ReadOnlyObservableCollection<FluidTag> Tags
        {
            get { return new ReadOnlyObservableCollection<FluidTag>(tags); }
        }

        /// <summary>
        /// Private backing collection for tags
        /// </summary>
        private ObservableCollection<FluidTag> tags { get; set; }

        /// <summary>
        /// Creates a new representation of an object in FluidDB
        /// Public interface must use static methods
        /// </summary>
        /// <param name="c">The instance of FluidDB</param>
        /// <param name="id">The ID of the object in FluidDB</param>
        private FluidObject(FluidConnector c, string id)
        {
            Connection = c;

            if (id != null)
                ID = new FluidDBGUID(id);
            tags = new ObservableCollection<FluidTag>();
        }

        /// <summary>
        /// This method creates a new object in FluidDB
        /// </summary>
        /// <param name="Connection">The instance of FluidDB to add the object to</param>
        /// <param name="about">A string to put into the immutable "about" tag</param>
        /// <returns>The created FluidDB object</returns>
        public static FluidObject CreateObject(FluidConnector Connection, string about)
        {
            Dictionary<string, object> body = new Dictionary<string, object>();
            if (about != string.Empty && about != null)
                body.Add("about", about);

            HttpWebResponse r = Connection.Call(METHOD.POST, "/objects", null, body);

            if (r.StatusCode == HttpStatusCode.Created)
            {                
                Dictionary<string, object> c = Connection.GetJsonResultDictionary(r);
                FluidObject o = new FluidObject(Connection, c["id"].ToString());   
                return o;
            }
            return null;
        }

        /// <summary>
        /// Gets the tags for the FluidDB object from the database
        /// The tags will populate the Tags collection
        /// They will not have their associated values or descriptions downloaded
        /// </summary>
        /// <param name="showAbout">Whether or not to retrieve the about string for the object</param>
        /// <returns>true on success, false on failure</returns>
        public bool GetTags(bool showAbout)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters.Add("showAbout", showAbout.ToString());

            HttpWebResponse r = Connection.Call(METHOD.GET, "/objects/" + ID, parameters, null);

            if (r.StatusCode == HttpStatusCode.OK)
            {
                Dictionary<string, object> c = Connection.GetJsonResultDictionary(r);
                if (showAbout)
                {
                    FluidTag about = new FluidTag(Connection, "about");
                    object value = c["about"];
                    if (value != null)
                        about.Description = value.ToString();
                    tags.Add(about);
                }

                foreach (string s in (c["tagPaths"] as ArrayList))
                {
                    tags.Add(new FluidTag(Connection, s));
                }
                return true;
            }
            else
            {
                return false;
            }
           
        }

        /// <summary>
        /// Queries FluidDB for a set of objects matching the query string
        /// </summary>
        /// <param name="Connection"></param>
        /// <param name="query"></param>
        /// <returns></returns>
        public static IEnumerable<FluidObject> GetObjects(FluidConnector Connection, string query)
        {
            List<FluidObject> list = new List<FluidObject>();
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters.Add("query", query);
            HttpWebResponse r = Connection.Call(METHOD.GET, "/objects", parameters, null);

            if (r.StatusCode == HttpStatusCode.OK)
            {
                Dictionary<string, object> d = Connection.GetJsonResultDictionary(r);

                foreach (string s in (d["ids"] as ArrayList))
                {
                    FluidObject o = new FluidObject(Connection, s);
                    list.Add(o);
                }

                return list;
            }

            return null;
        }

        /// <summary>
        /// Adds a created tag to an object with the specified value.
        /// This uses the primitive json type. (You can always tag the tag with the runtime type if needed).
        /// </summary>
        /// <param name="tag">The tag to tag the object with</param>
        /// <param name="value">The value to assign to the tag instance (ToString will be called)</param>
        public bool AddTag(FluidTag tag, object value)
        {
            System.Diagnostics.Debug.Assert(ID != null);

            if (tag == null)
                throw new ArgumentException("tag cannot be null!", "tag");

            if (value == null)
                throw new ArgumentException("value cannot be null!", "value");

            // I think serialization would be better here
            HttpWebResponse r = Connection.Call(METHOD.PUT, "/objects/" + ID + "/" + tag.Name, null, FluidConnector.PrimitivePutType, "\"" + value.ToString() + "\"");

            if (r.StatusCode == HttpStatusCode.NoContent)
            {
                tags.Add(tag);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Adds a created tag to an object with the specified value
        /// </summary>
        /// <param name="tag">The tag to tag the object with</param>
        /// <param name="contentType">The http Content-Type value to use</param>
        /// <param name="value">The value to assign to the tag instance</param>
        public void AddTag(FluidTag tag, string contentType, object value)
        {
            System.Diagnostics.Debug.Assert(ID != null);

            if (tag == null)
                throw new ArgumentException("tag cannot be null!", "tag");

            if (value == null)
                throw new ArgumentException("value cannot be null!", "value");

            // I think serialization would be better here
            // possibly need value + quotes
            Connection.Call(METHOD.PUT, "/objects/" + ID + "/" + tag.Name, null, contentType, value.ToString());

            tags.Add(tag);
        }

        /// <summary>
        /// Gets a FluidObject of a specified ID
        /// </summary>
        /// <param name="c"></param>
        /// <param name="p"></param>
        /// <returns></returns>
        public static FluidObject GetObject(FluidConnector c, FluidDBGUID id)
        {
            FluidObject o = new FluidObject(c, id.ID);
            return o;
        }
    }

}
