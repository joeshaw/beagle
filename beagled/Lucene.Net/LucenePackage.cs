/*
 * Copyright 2005 The Apache Software Foundation
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
using System;
namespace Lucene.Net
{
    /// <summary>
    /// Lucene's package information, including version. *
    /// </summary>
    public sealed class LucenePackage
    {
        private LucenePackage()
        {
        } // can't construct
		
        /// <summary>
        /// Return Lucene's package, including version information.
        /// </summary>
        // {{Aroush-1.9 how do we do this in C#
        //public static java.lang.Package Get()
        //{
        //    //UPGRADE_ISSUE: Method 'java.lang.Class.getPackage' was not converted. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1000_javalangClassgetPackage_3"'
        //    return typeof(LucenePackage).getPackage();
        //}
        // Aroush-1.9}}
    }
}