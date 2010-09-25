using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Collections;

namespace FluidDB.Extension
{
    /// <summary>
    /// The FluidDBObject attribute marks an object as being mapped to a FluidDB object
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class FluidDBObject : Attribute
    {
        /// <summary>
        /// This will get set once the object has been associated with a FluidDB object
        /// </summary>
        public FluidDBGUID ID { get; set; }

        /// <summary>
        /// Creates a new FluidDB object
        /// </summary>
        /// <param name="about">The description to put in the about tag for the object when saved</param>
        public FluidDBObject()
        {
        }
    }

    /// <summary>
    /// An attribute to mark properties of a .Net object (with FluidDBObject attribute) to show that they should be used as tags
    /// If the property type is marked with FluidDBObject then it will be mapped to FluidDB and linked by tag
    /// Otherwise ToString() will be called on the object
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class FluidDBTag : Attribute
    {
        /// <summary>
        /// A description of the tag (optional) 
        /// (not the value which comes from the value of the property this attribute is assigned to!)
        /// </summary>
        public string Description { get; private set; }

        /// <summary>
        /// </summary>
        /// <param name="description"></param>
        public FluidDBTag(string description)
        {
            Description = description;
        }
        /// <summary>
        /// 
        /// </summary>
        public FluidDBTag()
        {
        }

        internal static FluidTag GetDotNetTypeTag(FluidConnector c)
        {
            FluidNamespace n = FluidNamespace.GetNamespace(c, "guy127917", false, false, false);
            return FluidTag.GetTag(c, n, "DotNetTypeTag", false);
        }
    }

    /// <summary>
    /// An attribute to mark classes which can be serialized to a FluidDB tag value
    /// Allows a custom http Content-Type to be specified for object serialization
    /// 
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class FluidDBTagValue : Attribute
    {
        /// <summary>
        /// When serializing the value, this content type will be used in the http header
        /// </summary>
        public string ContentType { get; private set; }
    }
}
