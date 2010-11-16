using System;
using System.Threading;
using System.Web.UI;

namespace Spark.Web.Forms
{
	public class ViewControlBuilder : ControlBuilder
	{
		static int templateIndex;
		public override void Init(TemplateParser parser, ControlBuilder parentBuilder, Type type, string tagName, string id, System.Collections.IDictionary attribs)
		{
			attribs["TemplateIndex"] = Interlocked.Increment(ref templateIndex);
			base.Init(parser, parentBuilder, type, tagName, id, attribs);
		}
	}
}