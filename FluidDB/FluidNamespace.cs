using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
    class FluidNamespace
    {
    }
}
