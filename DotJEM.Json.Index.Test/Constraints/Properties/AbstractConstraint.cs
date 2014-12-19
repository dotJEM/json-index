#region Copyright (c) 2014 Systematic
// **********************************************************************************
// The MIT License (MIT)
// 
// Copyright (c) 2014 Systematic
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
// **********************************************************************************
#endregion

using System.IO;
using System.Text;
using NUnit.Framework.Constraints;

namespace DotJEM.Json.Index.Test.Constraints.Properties
{
    /// <summary>
    /// Base constraints for all Systematic Constraints.
    /// This should make it easier to write new constraints.
    /// </summary>
    public abstract class AbstractConstraint : Constraint
    {
        private bool result = true;
        private StringBuilder message;

        #region Core
        public override bool Matches(object actual)
        {
            base.actual = actual;
            message = new StringBuilder();
            DoMatches(actual);
            var currentResult = result;
            Reset();
            return currentResult;
        }

        protected abstract void DoMatches(object actual);

        /// <summary/>
        public override void WriteMessageTo(MessageWriter writer)
        {
            writer.WriteMessageLine(GetType().FullName + " Failed!");
            using (StringReader reader = new StringReader(message.ToString()))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                    writer.WriteMessageLine(line);
            }
        }

        /// <summary/>
        public override void WriteDescriptionTo(MessageWriter writer)
        {
        }
        #endregion

        /// <summary/>
        protected bool FailWithMessage(string format, params object[] args)
        {
            AppendLine(string.Format(format, args));
            return Fail();
        }

        /// <summary/>
        protected bool Fail()
        {
            return result = false;
        }

        /// <summary/>
        protected StringBuilder AppendFormat(string format, params object[] args)
        {
            return message.AppendFormat(format, args);
        }

        /// <summary/>
        protected StringBuilder AppendLine(string value)
        {
            return message.AppendLine(value);
        }

        /// <summary/>
        protected StringBuilder AppendLine()
        {
            return message.AppendLine();
        }

        private void Reset()
        {
            result = true;
        }
    }
}