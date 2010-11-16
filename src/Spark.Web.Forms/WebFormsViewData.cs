using System.Collections.Generic;

namespace Spark.Web.Forms
{
	public class WebFormsViewData : Dictionary<string, object>
	{
		public object Model { get; set; }

		public WebFormsViewData(object model, IDictionary<string, object> store)
			:base(store)
		{
			Model = model;
		}

		public object Eval(string key)
		{
			object value;
			return TryGetValue(key, out value) ? value : null;
		}
	}
}