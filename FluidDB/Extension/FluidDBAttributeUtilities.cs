using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace FluidDB.Extension
{
    /// <summary>
    /// Utility class to provide .Net object persistence using FluidDB
    /// 
    /// Example Usage:
    /// <code>
    /// Guy g = new Guy();
    /// FluidDBGUID id = g.WriteToFluidDB(FluidDB, g, "test");
    /// Guy p = FluidDBAttributeWriter.ReadFromFluidDB &lt;Guy>(FluidDB, "test", id);
    /// Guy o = (new Guy()).ReadFromFluidDB(FluidDB, "test", id);
    /// </code>
    /// </summary>
    public static class FluidDBAttributeUtilities
    {
        /// <summary>
        /// Writes a .NET object to the specified namespace in FluidDB
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="c"></param>
        /// <param name="namesp"></param>
        /// <returns></returns>
        public static FluidDBGUID WriteToFluidDB(this object obj, FluidConnector c, string namesp)
        {
            FluidNamespace f_namespace = FluidNamespace.GetNamespace(c, c.Username + "/" + namesp, false, false, false);
            if (f_namespace == null)
            {
                // if the user didnt bother creating the namespace, they can have default description!
                f_namespace = FluidNamespace.CreateNamespace(c, namesp, "default namespace description");
            }
            return WriteToFluidDB(obj, c, namesp);
        }
        /// <summary>
        /// Makes a .Net object persistent by writing to the FluidDB object
        /// </summary>
        /// <param name="c">The FluidDB connection to use</param>
        /// <param name="obj">The .Net object to write to FluidDB</param>
        /// <param name="f_namespace">The namespace for tags to be put in</param>
        /// <returns>An ID for retrieving the object</returns>
        public static FluidDBGUID WriteToFluidDB(this object obj, FluidConnector c, FluidNamespace f_namespace)
        {            
            object[] member_attributes = obj.GetType().GetCustomAttributes(typeof(FluidDBObject), false);
            foreach (object a in member_attributes)
            {
                // see if the attribute was a FluidDBObject
                FluidDBObject f_object = a as FluidDBObject;
                if (f_object == null)
                    continue;

                // if the object already exists, find it from fluid db
                FluidObject real_object;
                if (f_object.ID != null)
                {
                    real_object = FluidObject.GetObject(c, f_object.ID);
                }
                else
                {
                    // create an object in fluiddb to work with
                    // GB: look for a property marked with FluidDBAbout here
                    real_object = FluidObject.CreateObject(c, null);
                    f_object.ID = real_object.ID;
                }

                // we now have a valid fluid db object to work with.
                // lets tag it!

                // currently valid for tags are properties marked with the FluidDBTag attribute
                foreach (PropertyInfo property in obj.GetType().GetProperties())
                {
                    object[] property_attributes = property.GetCustomAttributes(false);
                    foreach (object attribute in property_attributes)
                    {
                        FluidDBTag tag = attribute as FluidDBTag;

                        // see if the attribute was a FluidDBTag
                        if (tag == null)
                            continue;
                        
                        // get value from property for description
                        object value = property.GetValue(obj, null);

                        // try and find the tag in fluid db
                        // if we didnt find it, create it
                        FluidTag real_tag = FluidTag.GetTag(c, f_namespace, property.Name, false);

                        // The description we have set comes from the attribute
                        // The value comes from the property
                        if (real_tag == null)
                        {
                            real_tag = FluidTag.CreateTag(c, f_namespace, property.Name, tag.Description, false);
                            FluidObject real_tag_object = FluidObject.GetObject(c, real_tag.ID);

                            // get the descriptor tag for the API
                            FluidTag value_type_tag = FluidDBTag.GetDotNetTypeTag(c);

                            // add a tag to the tag object with the .Net type name as the value
                            real_tag_object.AddTag(value_type_tag, value.GetType().Name);
                        }
                        //TODO check that the descriptor tag is on it and the correct type!
                        // TODO handle value == null case

                        //check if the value itself has a fluid db object attribute
                        //if so we map it to a seperate fluid db object 
                        object[] o_attrs = value.GetType().GetCustomAttributes(false);
                        bool foundAttr = false;
                        foreach (object object_attrib in o_attrs)
                        {
                            FluidDBObject f_propertyobject = object_attrib as FluidDBObject;

                            if (f_propertyobject != null)
                            {
                                // write this object to fluid db first
                                FluidDBGUID id = f_propertyobject.WriteToFluidDB(c, f_namespace.Name);
                                // now link to the object with the tag
                                real_object.AddTag(real_tag, id.ID);
                                foundAttr = true;
                                break;
                            }
                            else
                            {
                                // the value type has been marked with a custom content type
                                // this means we can serialize it
                                // GB I havent really worked this stuff out yet!
                                FluidDBTagValue tag_value = object_attrib as FluidDBTagValue;

                                if (tag_value != null)
                                {
                                    real_object.AddTag(real_tag, tag_value.ContentType, value);
                                }
                            }
                        }

                        // the property is not a FluidDBObject type and not marked with a custom Content-Type
                        // so we will assume it is a primitive and write it out as a string
                        if (!foundAttr)
                        {
                            if (!real_object.AddTag(real_tag, value.ToString()))
                            {
                                // failed to add tag
                                throw new Exception("Could not add tag to object");
                            }
                        }
                    }
                }

                return real_object.ID;
            }
            throw new ArgumentException("No FluidDBObject attribute found on object", "obj");
        }

        /// <summary>
        /// Reads data from object in FluidDB into the specified object with FluidDBObject attribute
        /// </summary>
        /// <param name="c">The connection to FluidDB to use</param>
        /// <param name="obj">The FluidDBObject object to read data into</param>
        /// <param name="ID">The ID of the created object in FluidDB</param>
        /// <returns></returns>
        public static void ReadFromFluidDB(this object obj, FluidConnector c, string f_namespace, FluidDBGUID ID)
        {
            object[] objectAttributes = obj.GetType().GetCustomAttributes(false);
            if (!objectAttributes.Any((o) => o is FluidDBObject))
            {
                throw new ArgumentException("No FluidDBObject attribute found on object", "obj");
            }

            // get the object from FluidDB (throws exception on failure to find)
            FluidObject real_obj = FluidObject.GetObject(c, ID);

            // Get the object tags (remember these may not all be relavent)
            // Tag values will not be retrieved here
            real_obj.GetTags(true);

            // Go through the properties and find what tags correspond to them
            PropertyInfo[] properties = obj.GetType().GetProperties();
            foreach (PropertyInfo property in properties)
            {
                if (property.GetCustomAttributes(false).Any((o) => o is FluidDBTag))
                {
                    // this property has the FluidDBTag, so lets get it some data
                    // find a tag with a matching namespace and tag name
                    string propertyName = c.Username + "/" + f_namespace + "/" + property.Name;
                    FluidTag tag = (from p in real_obj.Tags where propertyName.Equals(p.Name) select p).First();
                    tag.GetTagInformation();
                    // get the tags data and convert it back to data
                    object o = tag.GetValue(real_obj.ID);
                    property.SetValue(obj, o, null);
                }
            }
        }

        /// <summary>
        /// Uses the default constructor to construct a type from the FluidDB information
        /// based on the properties marked with the FluidDBTag attribute 
        /// </summary>
        /// <param name="c"></param>
        /// <param name="t"></param>
        /// <param name="f_namespace"></param>
        /// <param name="?"></param>
        public static T ReadFromFluidDB<T>(FluidConnector c, string f_namespace, FluidDBGUID id)
        {
            // get default constructor
            ConstructorInfo i = typeof(T).GetConstructor(new Type[] { });
            object o = i.Invoke(new object[] { });

            o.ReadFromFluidDB(c, f_namespace, id);

            return (T)o;
        }

        /// <summary>
        /// Returns a list of all objects from FluidDB of the specified .Net type (marked with FluidDBObject parameter)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="c"></param>
        /// <param name="f_namespace"></param>
        /// <returns></returns>
        public static IEnumerable<T> GetAllFromFluidDB<T>(FluidConnector c, string f_namespace)
        {
            // create a query which looks for appropriate objects
            object[] member_attributes = typeof(T).GetCustomAttributes(typeof(FluidDBObject), false);
            if (!member_attributes.Any((o) => o is FluidDBObject))
            {
                throw new Exception("Can only get classes with FluidDBObject attribute");
            }

            // get all properties marked with FluidDBTag or FluidDBObject (need to add further support here!)
            // get the data for those tags (using ReadFromFluidDB)
            var properties = from prop in typeof(T).GetProperties() where 
                        prop.GetCustomAttributes(false).Any((o) => o is FluidDBTag) select prop;

            string query = string.Join(" and has ", properties.Select((p) => c.Username + "/" + f_namespace + "/" + p.Name ).ToArray());

            IEnumerable<FluidObject> db_objects = FluidObject.GetObjects(c, "has " + query);
            return db_objects.Select( (obj) => ReadFromFluidDB<T>(c, f_namespace, obj.ID));
        }
    }
}
