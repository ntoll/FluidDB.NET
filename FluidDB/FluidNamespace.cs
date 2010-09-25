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
    /// FluidDB namespaces provide a simple hierarchical way of organizing names - names 
    /// of tags, and names of other (sub-)namespaces.
    ///
    /// When a new user is created within FluidDB, a top-level namespace is created for 
    /// them. For example, if Tim chooses the FluidDB user name tim, a top-level tim namespace 
    /// is created for him.
    /// 
    /// FluidDB user names are case insensitive.
    /// 
    /// Tim may then add a tag called rating within that namespace with the intention of 
    /// tagging objects with his ratings. With its name qualified by his namespace, Tim’s 
    /// rating tag can be unambiguously referred to as tim/rating. By using namespace and tag 
    /// names, with components separated by /, we can avoid any conflict or confusion with other 
    /// FluidDB rating tags, e.g., sara/rating.
    /// 
    /// Namespaces are hierarchical. Tim can later create a new namespace, for example books, 
    /// within his tim namespace, and in that namespace create an i-own tag. That tag would have 
    /// a full name of tim/books/i-own. Tim could use it to tag objects in FluidDB that correspond 
    /// to books he owns.
    /// 
    /// Because objects in FluidDB are not owned, another user, Sara, would be free to add her 
    /// own information to the book objects Tim had tagged. Thus an object might have both 
    /// tim/books/i-own and sara/rating tags on it, making it possible to ask FluidDB to find 
    /// books with a high Sara rating but which Tim does not own.
    /// </summary>
    public class FluidNamespace
    {
        private FluidConnector Connection {get; set;}

        /// <summary>
        /// Full name of this namespace from root
        /// </summary>
        public string Name { get; private set; }
        public string Description { get; private set; }
        public FluidDBGUID ID { get; private set; }
        public ObservableCollection<FluidTag> Tags { get; private set; }
        public ObservableCollection<FluidNamespace> Namespaces { get; private set; }

        private FluidNamespace(FluidConnector c, string name)
        {
            Connection = c;
            Name = name;

            Tags = new ObservableCollection<FluidTag>();
            Namespaces = new ObservableCollection<FluidNamespace>();
        }


        /// <summary>
        /// Gets information for specified namespace 
        /// <param name="c">The FluidConnector instance to make calls to</param>
        /// <param name="fl_namespace">The namespace to query, can be empty or null for top-level namespace</param>
        /// <param name="returnDescription">Whether to return a description of the namespace</param>
        /// <param name="returnNamespaces">Whether to return all namespaces within this namespace</param>
        /// <param name="returnTags">Whether or not to return all tags for this namespace</param>
        /// </summary>
        public static FluidNamespace GetNamespace(FluidConnector connection, string name, bool returnDescription, bool returnNamespaces, bool returnTags)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();

            parameters.Add("returnDescription", returnDescription.ToString());
            parameters.Add("returnNamespaces", returnNamespaces.ToString());
            parameters.Add("returnTags", returnTags.ToString());

            HttpWebResponse r = connection.Call(METHOD.GET, "/namespaces/" + name, parameters, null);
            
            if (r.StatusCode == HttpStatusCode.OK)
            {
                FluidNamespace n = new FluidNamespace(connection, name);
                Dictionary<string, object> response = connection.GetJsonResultDictionary(r);

                n.ID = new FluidDBGUID(response["id"] as string);

                if (returnDescription)
                {
                    n.Description = response["description"].ToString();
                }
                if (returnNamespaces)
                {
                    ArrayList namespaces = response["namespaceNames"] as ArrayList;
                    n.Namespaces.Clear();
                    foreach (object o in namespaces)
                    {
                        n.Namespaces.Add(new FluidNamespace(connection, o.ToString()));
                    }
                }
                if (returnTags)
                {
                    ArrayList tags = response["tagNames"] as ArrayList;
                    n.Tags.Clear();
                    foreach (object o in tags)
                    {
                       FluidTag t = new FluidTag(connection,  o.ToString());
                       n.Tags.Add(t);
                    }
                }

                return n;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Creates a new namespace in the current users namespace
        /// </summary>
        /// <param name="c"></param>
        /// <param name="namesp"></param>
        /// <param name="description"></param>
        internal static FluidNamespace CreateNamespace(FluidConnector c, string namesp, string description)
        {
            Dictionary<string, object> parameters = new Dictionary<string, object>();
            parameters.Add("description", description);
            parameters.Add("name", namesp);

            HttpWebResponse r = c.Call(METHOD.POST, "/namespaces/" + c.Username, null, parameters);

            if (r.StatusCode == HttpStatusCode.Created)
            {
                Dictionary<string, object> d= c.GetJsonResultDictionary(r);
                FluidNamespace n = new FluidNamespace(c, c.Username + "/" + namesp);
                n.ID = new FluidDBGUID(d["id"].ToString());
                return n;
            }

            return null;
        }
    }
}
