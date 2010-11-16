using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Web;
using System.Web.Configuration;
using System.Web.UI;
using System.IO;

namespace Spark.Web.Forms
{
    [ParseChildren(true, "InnerHtml")]
    [ControlBuilder(typeof(ViewControlBuilder))]
    public class View : Control
    {
        public View()
        {
            Data = new Dictionary<string, object>();
        }

        public string InnerHtml { get; set; }

        public string ModelProperty { get; set; }
        public string LayoutsPath { get; set; }
        public string SharedPath { get; set; }

        public string TemplateIndex { get; set; }
        internal string TemplateKey { get; set; }
        public IDictionary<string, object> Data { get; set; }
		internal SparkViewDescriptor Descriptor { get; set; }
		private WebFormsViewFactory Views { get; set; }

		protected override void OnInit(EventArgs e)
		{
			base.OnInit(e);

			TemplateKey = AppRelativeTemplateSourceDirectory.TrimStart('~', '/').Replace('/', '\\') + TemplateIndex + ".spark";
			Descriptor = GetViewDescriptor();

			Views = GetOrCreate(CreateViewEngine);
			Views.Add(this);
		}

		WebFormsViewFactory CreateViewEngine()
		{
			var pages = (PagesSection) WebConfigurationManager.GetSection("system.web/pages");
			var compilation = (CompilationSection)WebConfigurationManager.GetSection("system.web/compilation");
			return new WebFormsViewFactory(
				pages.Namespaces.OfType<NamespaceInfo>().Select(ni => ni.Namespace).ToList(),
				compilation.Assemblies.OfType<AssemblyInfo>().Select(ai => ai.Assembly).Where(a => a != "*").ToList()
				);
		}

		StringWriter viewBuffer = new StringWriter();
		protected override void OnPreRender(EventArgs e)
		{
			base.OnPreRender(e);

			Views.Render(this, viewBuffer);
		}
        protected override void Render(HtmlTextWriter writer)
        {
			Views.Dispose();
        	writer.Write(viewBuffer.ToString());
        }

		private SparkViewDescriptor GetViewDescriptor()
		{
			var descriptor = new SparkViewDescriptor().AddTemplate(TemplateKey);
			if (!string.IsNullOrEmpty(ModelProperty))
			{
				AddAccessor(descriptor, false, Page.GetType().GetProperty(ModelProperty).PropertyType, "Model", "base.ViewData.Model");
			}

			AddAccessor(descriptor, true, Page.GetType(), "Page", "base.Page");
			AddAccessor(descriptor, true, NamingContainer.GetType(), "Container", "base.Container");

			return descriptor;
		}

		private void AddAccessor(SparkViewDescriptor descriptor, bool isNew, Type modelType, string propertyName, string baseExpression)
		{
			if (modelType.GetCustomAttributes(typeof(CompilerGlobalScopeAttribute), false).Any())
				modelType = modelType.BaseType;
			descriptor.AddAccessor((isNew ? "new " : "") + modelType + " " + propertyName, baseExpression + " as " + modelType);
		}

        private T GetOrCreate<T>(Func<T> factory) where T : class
        {
            return Page.Items[typeof(T)] as T
                ?? (Page.Items[typeof(T)] = factory()) as T;
        }

        public object GetModel()
        {
            if (string.IsNullOrEmpty(ModelProperty))
                return null;
            return Page.GetType().BaseType.GetProperty(ModelProperty).GetValue(Page, null);
        }
    }
}