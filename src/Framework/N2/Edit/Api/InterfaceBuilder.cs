﻿using N2.Collections;
using N2.Edit;
using N2.Edit.Trash;
using N2.Engine;
using N2.Persistence;
using N2.Security;
using N2.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace N2.Management.Api
{
	public class Node<T>
	{
		public Node()
		{
			Children = new Node<T>[0];
		}

		public Node(T current)
			: this()
		{
			Current = current;
		}

		public T Current { get; set; }

		public IEnumerable<Node<T>> Children { get; set; }

		public bool HasChildren { get; set; }

		public bool Expanded { get; set; }
	}

	public class InterfaceMenuItem
	{
		public InterfaceMenuItem()
		{
			Target = Targets.Preview;
			RequiredPermission = Permission.Read;
		}

		public string Title { get; set; }
		public string Url { get; set; }
		public string Target { get; set; }

		public string IconClass { get; set; }

		public string Description { get; set; }

		public string ToolTip { get; set; }

		public string IconUrl { get; set; }

		public string Name { get; set; }

		public bool IsDivider { get; set; }

		public string TemplateUrl { get; set; }

		public Permission RequiredPermission { get; set; }

		public string HiddenBy { get; set; }

		public string DisplayedBy { get; set; }

		public string Alignment { get; set; }

		public string ClientAction { get; set; }

		public bool Divider { get; set; }
	}

	public class InterfaceData
	{
		public Node<InterfaceMenuItem> MainMenu { get; set; }

		public Node<InterfaceMenuItem> ToolbarMenu { get; set; }

		public Node<InterfaceMenuItem> ActionMenu { get; set; }

		public Node<TreeNode> Content { get; set; }

		public Site Site { get; set; }

		public string Authority { get; set; }

		public InterfaceUser User { get; set; }

		public InterfaceTrash Trash { get; set; }

		public InterfacePaths Paths { get; set; }

		public Node<InterfaceMenuItem> ContextMenu { get; set; }
	}

	public class InterfacePaths
	{
		public string Management { get; set; }

		public string Create { get; set; }

		public string Delete { get; set; }

		public string Edit { get; set; }

		public string SelectedQueryKey { get; set; }

		public string ViewPreference { get; set; }

		public string PreviewUrl { get; set; }
	}

	public class InterfaceUser
	{
		public string Name { get; set; }
		public string Username { get; set; }
		public ViewPreference PreferredView { get; set; }
	}

	public class InterfaceTrash : Node<N2.Edit.TreeNode>
	{
		public int TotalItems { get; set; }
		public int ChildItems { get; set; }
	}

	public class InterfaceBuiltEventArgs : EventArgs
	{
		public InterfaceData Data { get; internal set; }
	}

	[Service]
	public class InterfaceBuilder
	{
		private IEngine engine;

		public InterfaceBuilder(IEngine engine)
		{
			this.engine = engine;
		}

		public event EventHandler<InterfaceBuiltEventArgs> InterfaceBuilt;

		public virtual InterfaceData GetInterfaceContextData(HttpContextBase context, SelectionUtility selection)
		{
			var data = new InterfaceData
			{
				MainMenu = CreateMainMenu(),
				ActionMenu = CreateActionMenu(context),
				Content = CreateContent(context, selection),
				Site = engine.Host.GetSite(selection.SelectedItem),
				Authority = context.Request.Url.Authority,
				User = CreateUser(context),
				Trash = CreateTrash(context),
				Paths = CreateUrls(context, selection),
				ContextMenu = CreateContextMenu(context)
			};
			
			if (InterfaceBuilt != null)
				InterfaceBuilt(this, new InterfaceBuiltEventArgs { Data = data });

			return data;
		}

		private Node<InterfaceMenuItem> CreateContextMenu(HttpContextBase context)
		{
			return new Node<InterfaceMenuItem>
			{
				Children = engine.EditManager.GetPlugins<NavigationPluginAttribute>(context.User)
					.Where(np => !np.Legacy)
					.Select(np => CreateNode(np)).ToList()
			};
		}

		private Node<InterfaceMenuItem> CreateNode(LinkPluginAttribute np)
		{
			return new Node<InterfaceMenuItem>(new InterfaceMenuItem
			{
				Title = np.Title,
				Name = np.Name,
				Target = np.Target,
				ToolTip = np.ToolTip,
				IconUrl = string.IsNullOrEmpty(np.IconClass) ? Retoken(np.IconUrl) : null,
				Url = Retoken(np.UrlFormat),
				IsDivider = np.IsDivider,
				IconClass = np.IconClass
			});
		}

		static Dictionary<string, string> replacements = new Dictionary<string, string>
		{
			 { "{selected}", "{{Context.CurrentItem.Path}}" },
			 { "{memory}", "{{Context.Memory.Path}}" },
			 { "{action}", "{{Context.Memory.Action}}" },
			 { "{ManagementUrl}", Url.ResolveTokens("{ManagementUrl}") },
			 { "{Selection.SelectedQueryKey}", Url.ResolveTokens("{SelectedQueryKey}") }
		};

		private string Retoken(string urlFormat)
		{
			if (string.IsNullOrEmpty(urlFormat))
				return urlFormat;

			foreach (var kvp in replacements)
				urlFormat = urlFormat.Replace(kvp.Key, kvp.Value);
			return urlFormat;
		}

		protected virtual InterfacePaths CreateUrls(HttpContextBase context, SelectionUtility selection)
		{

			return new InterfacePaths
			{
				Management = engine.ManagementPaths.GetManagementInterfaceUrl(),
				Delete = engine.Config.Sections.Management.Paths.DeleteItemUrl.ResolveUrlTokens(),
				Edit = engine.Config.Sections.Management.Paths.EditItemUrl.ResolveUrlTokens(),
				SelectedQueryKey = engine.Config.Sections.Management.Paths.SelectedQueryKey.ResolveUrlTokens(),
				Create = engine.Config.Sections.Management.Paths.NewItemUrl.ResolveUrlTokens(),
				ViewPreference = context.GetViewPreference(engine.Config.Sections.Management.Versions.DefaultViewMode).ToString(),
				PreviewUrl = engine.GetContentAdapter<NodeAdapter>(selection.SelectedItem).GetPreviewUrl(selection.SelectedItem, allowDraft: true)
			};
		}

		protected virtual Node<InterfaceMenuItem> CreateActionMenu(HttpContextBase context)
		{
			var children = new List<Node<InterfaceMenuItem>>
			{
				new Node<InterfaceMenuItem>(new InterfaceMenuItem { Url = "{{Context.CurrentItem.PreviewUrl}}", Target = Targets.Preview, IconClass = "n2-icon-eye-open", HiddenBy = "Management" })
				{
					Children = new Node<InterfaceMenuItem>[]
					{
						new Node<InterfaceMenuItem>(new InterfaceMenuItem { Title = "Fullscreen", IconClass = "n2-icon-fullscreen", Target = Targets.Top, Url = "{{Context.CurrentItem.PreviewUrl}}" }),
						new Node<InterfaceMenuItem>(new InterfaceMenuItem { Divider = true }),
						new Node<InterfaceMenuItem>(new InterfaceMenuItem { Title = "View latest drafts", IconClass = "n2-icon-circle-blank", Target = Targets.Top, Url = "{ManagementUrl}/?view=draft&{SelectedQueryKey}={{Context.CurrentItem.Path}}&item={{Context.CurrentItem.ID}}".ResolveUrlTokens() }),
						new Node<InterfaceMenuItem>(new InterfaceMenuItem { Title = "View published versions", IconClass = "n2-icon-play-sign", Target = Targets.Top, Url = "{ManagementUrl}/?view=published&{SelectedQueryKey}={{Context.CurrentItem.Path}}&item={{Context.CurrentItem.ID}}".ResolveUrlTokens() }),
						new Node<InterfaceMenuItem>(new InterfaceMenuItem { Divider = true }),
						new Node<InterfaceMenuItem>(new InterfaceMenuItem { Title = "Show links", IconClass = "n2-icon-link", Target = Targets.Preview, Url = "{ManagementUrl}/Content/LinkTracker/Default.aspx?{SelectedQueryKey}={{Context.CurrentItem.Path}}&item={{Context.CurrentItem.ID}}".ResolveUrlTokens() }),
					}
				},
				new Node<InterfaceMenuItem>(new InterfaceMenuItem { TemplateUrl = "App/Partials/PageAdd.html", RequiredPermission = Permission.Write, HiddenBy = "Management" }),
				new Node<InterfaceMenuItem>(new InterfaceMenuItem { Title = "Edit", IconClass = "n2-icon-edit-sign", Target = Targets.Preview, Description = "Page details", Url = "{ManagementUrl}/Content/Edit.aspx?{SelectedQueryKey}={{Context.CurrentItem.Path}}&item={{Context.CurrentItem.ID}}&versionIndex={{Context.CurrentItem.VersionIndex}}".ResolveUrlTokens(), RequiredPermission = Permission.Write, HiddenBy = "Management" })
				{
					Children = new Node<InterfaceMenuItem>[]
					{
						new Node<InterfaceMenuItem>(new InterfaceMenuItem { Title = "Edit details", IconClass = "n2-icon-edit-sign", Target = Targets.Preview, Url = "{ManagementUrl}/Content/Edit.aspx?{SelectedQueryKey}={{Context.CurrentItem.Path}}&item={{Context.CurrentItem.ID}}&versionIndex={{Context.CurrentItem.VersionIndex}}".ResolveUrlTokens(), RequiredPermission = Permission.Write }),
						new Node<InterfaceMenuItem>(new InterfaceMenuItem { Title = "Organize parts", IconClass = "n2-icon-th-large", Target = Targets.Preview, Url = "{{Context.CurrentItem.PreviewUrl}}&edit=drag", RequiredPermission = Permission.Write }),
						new Node<InterfaceMenuItem>(new InterfaceMenuItem { Divider = true }),
						new Node<InterfaceMenuItem>(new InterfaceMenuItem { Title = "Manage security", IconClass = "n2-icon-lock", Target = Targets.Preview, Url = "{ManagementUrl}/Content/Security/Default.aspx?{SelectedQueryKey}={{Context.CurrentItem.Path}}&item={{Context.CurrentItem.ID}}".ResolveUrlTokens(), RequiredPermission = Permission.Administer }),
						new Node<InterfaceMenuItem>(new InterfaceMenuItem { Title = "Bulk editing", IconClass = "n2-icon-edit", Target = Targets.Preview, Url = "{ManagementUrl}/Content/Export/BulkEditing.aspx?{SelectedQueryKey}={{Context.CurrentItem.Path}}&item={{Context.CurrentItem.ID}}".ResolveUrlTokens(), RequiredPermission = Permission.Publish }),
					}
				},
				new Node<InterfaceMenuItem>(new InterfaceMenuItem { IconClass = "n2-icon-trash", Url = "{ManagementUrl}/Content/Delete.aspx?{SelectedQueryKey}={{Context.CurrentItem.Path}}&item={{Context.CurrentItem.ID}}".ResolveUrlTokens(), ToolTip = "Throw selected item", RequiredPermission = Permission.Publish, HiddenBy = "Management" }),
				new Node<InterfaceMenuItem>(new InterfaceMenuItem { TemplateUrl = "App/Partials/PageVersions.html", Url = "{ManagementUrl}/Content/Versions/?{SelectedQueryKey}={{Context.CurrentItem.Path}}&item={{Context.CurrentItem.ID}}".ResolveUrlTokens(), RequiredPermission = Permission.Publish, HiddenBy = "Management" }),
				new Node<InterfaceMenuItem>(new InterfaceMenuItem { TemplateUrl = "App/Partials/PageLanguage.html", Url = "{ManagementUrl}/Content/Globalization/?{SelectedQueryKey}={{Context.CurrentItem.Path}}&item={{Context.CurrentItem.ID}}".ResolveUrlTokens(), RequiredPermission = Permission.Write, HiddenBy = "Management" }),
				new Node<InterfaceMenuItem>(new InterfaceMenuItem { TemplateUrl = "App/Partials/PagePublish.html", RequiredPermission = Permission.Write, DisplayedBy = "Unpublished", HiddenBy = "Management", Alignment = "Right" })
				{
					Children = new Node<InterfaceMenuItem>[]
					{
						new Node<InterfaceMenuItem>(new InterfaceMenuItem { Title = "Publish", IconClass = "n2-icon-play-sign", ClientAction = "publish()", RequiredPermission = Permission.Publish, HiddenBy = "Published" }),
						new Node<InterfaceMenuItem>(new InterfaceMenuItem { TemplateUrl = "App/Partials/PagePublishSchedule.html", RequiredPermission = Permission.Publish, DisplayedBy = "Draft" }),
						new Node<InterfaceMenuItem>(new InterfaceMenuItem { Title = "Unpublish", IconClass = "n2-icon-stop", ClientAction = "unpublish()", RequiredPermission = Permission.Publish, DisplayedBy = "Published" }),
						new Node<InterfaceMenuItem>(new InterfaceMenuItem { Title = "Info", IconClass = "n2-icon-info-sign", ClientAction = "toggleInfo()" }),
					}
				},
				new Node<InterfaceMenuItem>(new InterfaceMenuItem { TemplateUrl = "App/Partials/FrameAction.html", RequiredPermission = Permission.Write }),
				new Node<InterfaceMenuItem>(new InterfaceMenuItem { Title = "Close", /*IconClass = "n2-icon-check-minus", */Url = "{{Context.CurrentItem.PreviewUrl || Context.Paths.PreviewUrl}}", Target = Targets.Preview, DisplayedBy = "Management" }),
			};

			children.AddRange(engine.EditManager.GetPlugins<ToolbarPluginAttribute>(context.User)
					.Where(np => !np.Legacy)
					.Select(np => CreateNode(np)));
			
			return new Node<InterfaceMenuItem>
			{
				Children = children
			};
		}

		protected virtual InterfaceTrash CreateTrash(HttpContextBase context)
		{
			var trash = engine.Resolve<ITrashHandler>();

			if (trash.TrashContainer == null)
				return new InterfaceTrash();

			var container = (ContentItem)trash.TrashContainer;

			var total = (int)engine.Persister.Repository.Count(Parameter.Below(container));
			var children = (int)engine.Persister.Repository.Count(Parameter.Equal("Parent", container));

			return new InterfaceTrash
			{
				Current = engine.GetContentAdapter<NodeAdapter>(container).GetTreeNode(container),
				HasChildren = children > 0,
				ChildItems = children,
				TotalItems = total,
				Children = new Node<TreeNode>[0]
			};
		}

		protected virtual InterfaceUser CreateUser(HttpContextBase context)
		{
			return new InterfaceUser
			{
				Name = context.User.Identity.Name,
				Username = context.User.Identity.Name,
				PreferredView = engine.Config.Sections.Management.Versions.DefaultViewMode
			};
		}

		protected virtual Node<TreeNode> CreateContent(HttpContextBase context, SelectionUtility selection)
		{
			var filter = engine.EditManager.GetEditorFilter(context.User);

			var structure = new BranchHierarchyBuilder(selection.SelectedItem, selection.Traverse.RootPage, true) { UseMasterVersion = false }
				.Children((item) => engine.GetContentAdapter<NodeAdapter>(item).GetChildren(item, Interfaces.Managing).Where(filter))
				.Build();

			return CreateStructure(structure, filter);
		}

		protected virtual Node<TreeNode> CreateStructure(HierarchyNode<ContentItem> structure, ItemFilter filter)
		{
			var adapter = engine.GetContentAdapter<NodeAdapter>(structure.Current);

			var children = structure.Children.Select(c => CreateStructure(c, filter)).ToList();
			return new Node<TreeNode>
			{
				Current = adapter.GetTreeNode(structure.Current),
				HasChildren = adapter.HasChildren(structure.Current, filter),
				Expanded = children.Any(),
				Children = children
			};
		}

		protected virtual Node<InterfaceMenuItem> CreateMainMenu()
		{
			return new Node<InterfaceMenuItem>
			{
				Children = new Node<InterfaceMenuItem>[]
				{
					new Node<InterfaceMenuItem>(new InterfaceMenuItem { Title = "Dashboard", IconClass = "n2-icon-home" , Target = Targets.Preview, Url = engine.Content.Traverse.RootPage.Url }),
					new Node<InterfaceMenuItem>(new InterfaceMenuItem { Title = "Pages", IconClass = "n2-icon-edit", Target = "_top", Url = "{ManagementUrl}".ResolveUrlTokens() }),
					
					new Node<InterfaceMenuItem>(new InterfaceMenuItem { Divider = true }),
					
					new Node<InterfaceMenuItem>(new InterfaceMenuItem { Title = "Site Settings", IconClass = "n2-icon-cog", ToolTip = "Edit site settings", Target = Targets.Preview, Url = "{ManagementUrl}/Content/EditRecursive.aspx?{SelectedQueryKey}={{Context.CurrentItem.Path}}&id={{Context.CurrentItem.ID}}".ResolveUrlTokens(), RequiredPermission = Permission.Write }),
					new Node<InterfaceMenuItem>(new InterfaceMenuItem { Title = "Templates", IconClass = "n2-icon-plus-sign-alt", ToolTip = "Show predefined templates with content", Target = Targets.Preview, Url = "{ManagementUrl}/Content/Templates/Default.aspx?{SelectedQueryKey}={{Context.CurrentItem.Path}}&item={{Context.CurrentItem.ID}}".ResolveUrlTokens().ResolveUrlTokens(), RequiredPermission = Permission.Administer }),
					new Node<InterfaceMenuItem>(new InterfaceMenuItem { Title = "Wizards", IconClass = "n2-icon-magic", ToolTip = "Show predefined types and locations for content", Target = Targets.Preview, Url = "{ManagementUrl}/Content/Wizard/Default.aspx?{SelectedQueryKey}={{Context.CurrentItem.Path}}&item={{Context.CurrentItem.ID}}".ResolveUrlTokens() }),
					new Node<InterfaceMenuItem>(new InterfaceMenuItem { Title = "Users", IconClass = "n2-icon-user", ToolTip = "Manage users", Target = Targets.Preview, Url = "{ManagementUrl}/Users/Users.aspx".ResolveUrlTokens(), RequiredPermission = Permission.Administer }),
					new Node<InterfaceMenuItem>(new InterfaceMenuItem { Title = "Export", IconClass = "n2-icon-cloud-download", ToolTip = "Export selected content", Target = Targets.Preview, Url = "{ManagementUrl}/Content/Export/Export.aspx?{SelectedQueryKey}={{Context.CurrentItem.Path}}&item={{Context.CurrentItem.ID}}".ResolveUrlTokens(), RequiredPermission = Permission.Administer }),
					new Node<InterfaceMenuItem>(new InterfaceMenuItem { Title = "Import", IconClass = "n2-icon-cloud-upload", ToolTip = "Import content", Target = Targets.Preview, Url = "{ManagementUrl}/Content/Export/Default.aspx?{SelectedQueryKey}={{Context.CurrentItem.Path}}&item={{Context.CurrentItem.ID}}".ResolveUrlTokens(), RequiredPermission = Permission.Administer }),

					new Node<InterfaceMenuItem>(new InterfaceMenuItem { Divider = true }),

					new Node<InterfaceMenuItem>(new InterfaceMenuItem { Title = "Sign out", IconClass = "n2-icon-signout", ToolTip = "Sign out {{Context.User.Name}}", Url = "{ManagementUrl}/Login.aspx?logout=true".ResolveUrlTokens() }),
				}
			};
		}
	}
}