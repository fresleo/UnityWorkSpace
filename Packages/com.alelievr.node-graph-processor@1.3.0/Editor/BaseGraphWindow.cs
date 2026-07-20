using System.Linq;
using System;
using GameEditor.NodeFlow;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

namespace GraphProcessor
{
	[System.Serializable]
	public abstract class BaseGraphWindow : EditorWindow
	{
		protected VisualElement		rootView;
		protected BaseGraphView		graphView;

		[SerializeField]
		protected BaseGraph			graph;

		readonly string				graphWindowStyle = "GraphProcessorStyles/BaseGraphView";

		public bool					isGraphLoaded
		{
			get { return graphView != null && graphView.graph != null; }
		}

		bool						reloadWorkaround = false;

		public event Action< BaseGraph >	graphLoaded;
		public event Action< BaseGraph >	graphUnloaded;

		/// <summary>
		/// Called by Unity when the window is enabled / opened
		/// </summary>
		protected virtual void OnEnable()
		{
			InitializeRootView();

			graphLoaded = (baseGraph) =>
			{
				if (baseGraph != null)
				{
					baseGraph.OnGraphEnable();
				}
			};

			graphUnloaded = (baseGraph) =>
			{
				if(baseGraph != null)
				{
					baseGraph.OnGraphDisable();
				}
			};
			
			reloadWorkaround = true;
		}

		protected virtual void Update()
		{
			// Workaround for the Refresh option of the editor window:
			// When Refresh is clicked, OnEnable is called before the serialized data in the
			// editor window is deserialized, causing the graph view to not be loaded
			if (reloadWorkaround && graph != null)
			{
				//LoadGraph();
				reloadWorkaround = false;
			}
		}

		/// <summary>
		/// Called by Unity when the window is disabled (happens on domain reload)
		/// </summary>
		protected virtual void OnDisable()
		{
			if (graph != null && graphView != null)
			{
				graphView.SaveGraphToDisk();

				if (graphUnloaded != null)
				{
					graphUnloaded.Invoke(graph);
					graph = null;
				}
			}
		}

		/// <summary>
		/// Called by Unity when the window is closed
		/// </summary>
		protected virtual void OnDestroy()
		{
			if (graphView != null)
			{
				graphView.Dispose();
			}
		}

		void InitializeRootView()
		{
			rootView = base.rootVisualElement;

			rootView.name = "graphRootView";

			rootView.styleSheets.Add(Resources.Load<StyleSheet>(graphWindowStyle));
		}

		public void InitializeGraph(BaseGraph graph)
		{
			if (this.graph != null && graph != this.graph)
			{
				NodeGraphUtils.SaveGraph(graph);
				// Unload the graph
				graphUnloaded?.Invoke(this.graph);
			}

			graphLoaded?.Invoke(graph);
			this.graph = graph;

			if (graphView != null)
				rootView.Remove(graphView);

			//Initialize will provide the BaseGraphView
			InitializeWindow(graph);
			rootView.Add(graphView);

			graphView = rootView.Children().FirstOrDefault(e => e is BaseGraphView) as BaseGraphView;

			if (graphView == null)
			{
				Debug.LogError("GraphView has not been added to the BaseGraph root view !");
				return ;
			}

			graphView.Initialize(graph);

			InitializeGraphView(graphView);

			// TOOD: onSceneLinked...

			if (graph.IsLinkedToScene())
				LinkGraphWindowToScene(graph.GetLinkedScene());
			else
				graph.onSceneLinked += LinkGraphWindowToScene;

			reloadWorkaround = false;
		}
		
		public void RefrushGraphWindow()
		{
			reloadWorkaround = true;
		}

		void LinkGraphWindowToScene(Scene scene)
		{
			EditorSceneManager.sceneClosed += CloseWindowWhenSceneIsClosed;

			void CloseWindowWhenSceneIsClosed(Scene closedScene)
			{
				if (scene == closedScene)
				{
					Close();
					EditorSceneManager.sceneClosed -= CloseWindowWhenSceneIsClosed;
				}
			}
		}

		public virtual void OnGraphDeleted()
		{
			if (graph != null && graphView != null)
				rootView.Remove(graphView);

			graphView = null;
		}

		protected abstract void	InitializeWindow(BaseGraph graph);
		protected virtual void InitializeGraphView(BaseGraphView view) {}
	}
}