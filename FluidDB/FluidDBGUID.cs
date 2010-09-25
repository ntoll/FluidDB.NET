using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FluidDB
{
    /// <summary>
    /// Type safe way of passing ID's around.
    /// Also allows complete abstraction from implementation for user.
    /// </summary>
    public class FluidDBGUID
    {
        /// <summary>
        /// The global unique identifier string for a FluidDB object
        /// </summary>
        internal string ID { get; private set; }

        /// <summary>
        /// Creates a new encapsulated object string
        /// </summary>
        /// <param name="id"></param>
        internal FluidDBGUID(string id)
        {
            ID = id;
        }

        /// <summary>
        /// Public method for getting ID string
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return ID;
        }

        public override bool Equals(object obj)
        {
            FluidDBGUID id = obj as FluidDBGUID;
            if (id != null && id.ID.Equals(ID))
            {
                return true;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return ID.GetHashCode();
        }
    }
}
