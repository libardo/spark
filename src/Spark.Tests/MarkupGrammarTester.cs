/*
   Copyright 2008 Louis DeJardin - http://whereslou.com

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Spark.Parser;
using Spark.Parser.Markup;
using NUnit.Framework;

namespace Spark.Tests
{
    [TestFixture]
    public class MarkupGrammarTester
    {
        private MarkupGrammar grammar;

        [SetUp]
        public void Init()
        {
            grammar = new MarkupGrammar();
        }

        private Position Source(string content)
        {
            return new Position(new SourceContext(content));
        }

        [Test]
        public void RepDigits()
        {
            ParseAction<char> digit =
                delegate(Position input)
                {
                    if (input.PotentialLength() == 0 || !char.IsDigit(input.Peek())) return null;
                    return new ParseResult<char>(input.Advance(1), input.Peek());
                };

            var digits = digit.Rep();

            var result = digits(Source("55407"));
            Assert.AreEqual(0, result.Rest.PotentialLength());
            Assert.AreEqual("55407", new String(result.Value.ToArray()));
        }

        [Test]
        public void TextNode()
        {
            var result = grammar.Text(Source("hello world"));
            Assert.AreEqual("hello world", result.Value.Text);

            var result2 = grammar.Text(Source("hello&nbsp;world"));
            Assert.AreEqual("hello", result2.Value.Text);
        }

        [Test]
        public void EntityNode()
        {
            var result = grammar.EntityRef(Source("&lt;"));
            Assert.AreEqual("lt", result.Value.Name);

            var result2 = grammar.EntityRef(Source("&lt;world"));
            Assert.AreEqual("lt", result.Value.Name);

            var result3 = grammar.EntityRef(Source("hello&lt;world"));
            Assert.IsNull(result3);
        }

        [Test]
        public void Rep1WontBeNone()
        {
            var parser = CharGrammar.Ch('x').Rep1();
            var three = parser(Source("xxx5"));
            Assert.IsNotNull(three);
            Assert.AreEqual(3, three.Value.Count);

            var nada = parser(Source("yxxx"));
            Assert.IsNull(nada);
        }

        [Test]
        public void EntityTextSeries()
        {
            var result = grammar.Nodes(Source("hello&nbsp;world"));
            Assert.IsNotNull(result);
            Assert.AreEqual(3, result.Value.Count);
            Assert.IsAssignableFrom(typeof(TextNode), result.Value[0]);
            Assert.IsAssignableFrom(typeof(EntityNode), result.Value[1]);
            Assert.IsAssignableFrom(typeof(TextNode), result.Value[2]);
        }

        [Test]
        public void ParsingAttribute()
        {
            var result = grammar.Attribute(Source("foo=\"quad\""));
            Assert.IsNotNull(result);
            Assert.AreEqual("foo", result.Value.Name);
            Assert.AreEqual("quad", result.Value.Value);

            var result2 = grammar.Attribute(Source("foo2='quad2'"));
            Assert.IsNotNull(result2);
            Assert.AreEqual("foo2", result2.Value.Name);
            Assert.AreEqual("quad2", result2.Value.Value);

            var result3 = grammar.Attribute(Source("foo3!='quad2'"));
            Assert.IsNull(result3);
        }

        [Test]
        public void ParsingElement()
        {
            var result = grammar.Element(Source("<blah>"));
            Assert.IsNotNull(result);
            Assert.AreEqual("blah", result.Value.Name);
        }

        [Test]
        public void ParsingElementWithAttributes()
        {
            var result = grammar.Element(Source("<blah foo=\"quad\" omg=\"w00t\">"));
            Assert.IsNotNull(result);
            Assert.AreEqual("blah", result.Value.Name);
            Assert.AreEqual(2, result.Value.Attributes.Count);
            Assert.AreEqual("foo", result.Value.Attributes[0].Name);
            Assert.AreEqual("quad", result.Value.Attributes[0].Value);
            Assert.AreEqual("omg", result.Value.Attributes[1].Name);
            Assert.AreEqual("w00t", result.Value.Attributes[1].Value);
        }

        [Test]
        public void ParsingEndElement()
        {
            var result = grammar.EndElement(Source("</blah>"));
            Assert.IsNotNull(result);
            Assert.IsAssignableFrom(typeof(EndElementNode), result.Value);
            Assert.AreEqual("blah", result.Value.Name);
        }

        [Test]
        public void PassingSimpleMarkup()
        {
            var result = grammar.Nodes(Source("<foo><bar>one</bar><quad a='1' b='2'>55</quad></foo>"));
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Rest.PotentialLength());
            Assert.AreEqual(8, result.Value.Count);
            Assert.IsAssignableFrom(typeof(ElementNode), result.Value[4]);
            var elt = result.Value[4] as ElementNode;
            Assert.AreEqual("quad", elt.Name);
            Assert.AreEqual(2, elt.Attributes.Count);
            Assert.AreEqual("2", elt.Attributes[1].Value);
        }

        [Test]
        public void SelfEnding()
        {
            var result = grammar.Nodes(Source("<div><br/></div>"));
            Assert.IsAssignableFrom(typeof(ElementNode), result.Value[0]);
            Assert.IsAssignableFrom(typeof(ElementNode), result.Value[1]);
            Assert.IsAssignableFrom(typeof(EndElementNode), result.Value[2]);

            var div = result.Value[0] as ElementNode;
            Assert.AreEqual("div", div.Name);
            Assert.That(!div.IsEmptyElement);

            var br = result.Value[1] as ElementNode;
            Assert.AreEqual("br", br.Name);
            Assert.That(br.IsEmptyElement);

            var ediv = result.Value[2] as EndElementNode;
            Assert.AreEqual("div", ediv.Name);
        }

        [Test]
        public void DoctypeParser()
        {
            var result =
                grammar.DoctypeDecl(
                    Source(
                        "<!DOCTYPE html PUBLIC \"-//W3C//DTD XHTML 1.0 Strict//EN\" \"http://www.w3.org/TR/2002/REC-xhtml1-20020801/DTD/xhtml1-strict.dtd\">"));

            Assert.IsNotNull(result);
            Assert.AreEqual("html", result.Value.Name);
            Assert.AreEqual("PUBLIC", result.Value.ExternalId.ExternalIdType);
            Assert.AreEqual("-//W3C//DTD XHTML 1.0 Strict//EN", result.Value.ExternalId.PublicId);
            Assert.AreEqual("http://www.w3.org/TR/2002/REC-xhtml1-20020801/DTD/xhtml1-strict.dtd", result.Value.ExternalId.SystemId);

            var result2 =
                grammar.DoctypeDecl(
                    Source(
                        "<!DOCTYPE html PUBLIC \"http://www.w3.org/TR/2002/REC-xhtml1-20020801/DTD/xhtml1-strict.dtd\">"));
            Assert.IsNull(result2);

            var result3 =
                grammar.DoctypeDecl(
                    Source(
                        "<!DOCTYPE html SYSTEM 'hello world'>"));
            Assert.IsNotNull(result);
            Assert.AreEqual("html", result3.Value.Name);
            Assert.AreEqual("SYSTEM", result3.Value.ExternalId.ExternalIdType);
            Assert.AreEqual("hello world", result3.Value.ExternalId.SystemId);

            var result4 =
                grammar.DoctypeDecl(
                    Source(
                        "<!DOCTYPE foo >"));
            Assert.IsNotNull(result4);
            Assert.AreEqual("foo", result4.Value.Name);
            Assert.IsNull(result4.Value.ExternalId);
        }

        [Test]
        public void CodeInText()
        {
            var result = grammar.Nodes(Source("<hello>foo${bar}ex</hello>"));
            Assert.IsNotNull(result);
            Assert.AreEqual(5, result.Value.Count);
            Assert.IsAssignableFrom(typeof(ExpressionNode), result.Value[2]);
            var code = result.Value[2] as ExpressionNode;
            Assert.AreEqual("bar", code.Code);

            result = grammar.Nodes(Source("<hello>foo$baaz;ex</hello>"));
            Assert.IsNotNull(result);
            Assert.AreEqual(5, result.Value.Count);
            Assert.IsAssignableFrom(typeof(ExpressionNode), result.Value[2]);
            var code2 = result.Value[2] as ExpressionNode;
            Assert.AreEqual("baaz", code2.Code);

            result = grammar.Nodes(Source("<hello href='${one}' class=\"$two;\"/>"));
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Value.Count);
            Assert.IsAssignableFrom(typeof(ElementNode), result.Value[0]);
            var elt = result.Value[0] as ElementNode;
            Assert.AreEqual("one", (elt.Attributes[0].Nodes[0] as ExpressionNode).Code);
            Assert.AreEqual("two", (elt.Attributes[1].Nodes[0] as ExpressionNode).Code);

        }

        [Test]
        public void CommentParser()
        {
            var result = grammar.Comment(Source("<!-- hello world -->"));
            Assert.IsNotNull(result);
            Assert.AreEqual(" hello world ", result.Value.Text);

            var result2 = grammar.Comment(Source("<!-- hello-world -->"));
            Assert.IsNotNull(result2);
            Assert.AreEqual(" hello-world ", result2.Value.Text);

            var result3 = grammar.Comment(Source("<!-- hello--world -->"));
            Assert.IsNull(result3);
        }
    }
}