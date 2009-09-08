using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
    class FluidObject
    {
    }
}
