using System;
using System.Web;
using System.Web.UI;

namespace Spark.Web.Forms
{
	public abstract class WebFormsSparkView : SparkViewBase
	{
		public Control Container { get; set; }
		public Page Page { get; set; }
		public WebFormsViewData ViewData { get; set; }

		public virtual void Initialize(View viewControl)
		{
			ViewData = new WebFormsViewData(viewControl.GetModel(), viewControl.Data);
			Page = viewControl.Page;
			Container = viewControl.NamingContainer;
		}

		public string SiteRoot
		{
			get { return Page.Request.ApplicationPath; }
		}

		public string SiteResource(string path)
		{
			return SiteRoot + path.TrimStart('~', '/');
		}

		public override bool TryGetViewData(string name, out object value)
		{
			return ViewData.TryGetValue(name, out value);
		}

		public string H(object content)
		{
			return HttpUtility.HtmlEncode(Convert.ToString(content));
		}

		public object HTML(object value)
		{
			return value;
		}

		public virtual object Eval(string expression)
		{
			return ViewData.Eval(expression);
		}
	}
}