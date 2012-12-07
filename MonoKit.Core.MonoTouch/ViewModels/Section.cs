//  --------------------------------------------------------------------------------------------------------------------
//  <copyright file="Section.cs" company="sgmunn">
//    (c) sgmunn 2012  
//
//    Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated 
//    documentation files (the "Software"), to deal in the Software without restriction, including without limitation 
//    the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and 
//    to permit persons to whom the Software is furnished to do so, subject to the following conditions:
//
//    The above copyright notice and this permission notice shall be included in all copies or substantial portions of 
//    the Software.
//
//    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//    THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
//    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF 
//    CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS 
//    IN THE SOFTWARE.
//  </copyright>
//  --------------------------------------------------------------------------------------------------------------------

namespace MonoKit.ViewModels
{
    using System;
    using System.Collections;
    using System.Collections.Generic;

    /// <summary>
    /// Default implementation of an ISection
    /// </summary>
    public sealed class Section : ISection
    {
        public Section()
        {
            this.Items = new List<object>();
        }

        public Section(ISection source)
        {
            this.Header = source.Header;
            this.Footer = source.Footer;
            this.Items = new List<object>();
            foreach (var item in source.Items)
            {
                this.Items.Add(item);
            }
        }

        public object Header { get; set; }
        
        public object Footer { get; set; }
        
        public IList Items { get; private set; }
        
        public object this[int index] 
        { 
            get
            {
                return this.Items[index];
            }
        }
    }
}
