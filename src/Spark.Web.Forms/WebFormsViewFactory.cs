using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Spark.Caching;
using Spark.FileSystem;

namespace Spark.Web.Forms
{
	public class WebFormsViewFactory : List<View>, IDisposable
	{
		private readonly IEnumerable<string> namespaces;
		private readonly IEnumerable<string> assemblies;

		public WebFormsViewFactory(IEnumerable<string> namespaces, IEnumerable<string> assemblies)
		{
			this.namespaces = namespaces;
			this.assemblies = assemblies;
		}

		private SparkViewEngine engine;

		public SparkViewEngine Engine()
		{
			if (engine != null)
				return engine;

			var settings = new SparkSettings()
				.SetPageBaseType(typeof(WebFormsSparkView))
				.SetNullBehaviour(NullBehaviour.Strict);
			foreach (var ns in namespaces)
			{
				settings = settings.AddNamespace(ns);
			}
			foreach (var a in assemblies)
			{
				settings = settings.AddAssembly(a);
			}

			engine = new SparkViewEngine(settings);

			engine.ViewFolder = GetViewFolder();
			
			engine.BatchCompilation(GetViewDescriptors());
			return engine;
		}


		private IViewFolder GetViewFolder()
		{
			var folder = new InMemoryViewFolder();
			foreach (var c in this)
			{
				folder.Add(c.TemplateKey, c.InnerHtml);
			}
			return folder;
		}

		private List<SparkViewDescriptor> GetViewDescriptors()
		{
			return this.Select(c => c.Descriptor).ToList();
		}

		private IDisposable renderer = null;
		private SparkViewContext context = new SparkViewContext();
		public void Render(View viewControl, TextWriter writer)
		{
			var entry = Engine().GetEntry(viewControl.Descriptor);
			var view = (WebFormsSparkView)entry.CreateInstance();
			view.Initialize(viewControl);
			view.CacheService = new DefaultCacheService(viewControl.Page.Cache);

			view.SparkViewContext = context;
			if (renderer == null)
			{
				renderer = view.OutputScope(writer);
			}
			view.Output = writer;
			
			view.Render();
		}

		#region IDisposable Members

		public void Dispose()
		{
			if (renderer != null)
				renderer.Dispose();
			renderer = null;
		}

		#endregion
	}
}