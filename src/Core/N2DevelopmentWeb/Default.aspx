<%@ Page Theme="Theme1" MasterPageFile="~/DefaultMasterPage.Master" Language="C#" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="N2.TemplateWeb.Default" %>
<%@ OutputCache CacheProfile="DefaultCache" %>
<asp:Content ID="Content1" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">

<textarea style="height:100px; width:30%;">
trimming:	<%= System.Web.SiteMap.Provider.SecurityTrimmingEnabled %>
id:		<%= CurrentPage.ID %>
root:		<%= N2.Find.RootItem.ID %>
start:		<%= N2.Find.StartPage.ID%>
current:	<%= N2.Find.CurrentPage.ID%>
parent:		<%= N2.Utility.Evaluate(CurrentPage, "Parent.ID") %>
#children:	<%= CurrentPage.Children.Count %>
#total:		<%= N2.Find.Items.All.Count() %>
name :		<asp:Literal runat="server" Text="<%$ Code: CurrentPage.Name %>" />
</textarea>


<textarea style="height:100px; width:30%;">
AbsolutePath		<%= WC.AbsolutePath			%>
ApplicationUrl		<%= WC.ApplicationUrl		%>
ContentPage		<%= WC.CurrentPage			%>
Cookies.Count		<%= WC.Cookies.Count		%>
Handler.GetType()	<%= WC.Handler.GetType()	%>
Host			<%= WC.Host					%>
IsInWebContext		<%= WC.IsInWebContext		%>
RawUrl			<%= WC.RawUrl				%>
QueryString			<%= WC.Query				%>
RequestItems.Count	<%= WC.RequestItems.Count	%>
User.Identity.Name	<%= WC.User.Identity.Name	%>
PhysicalPath		<%= WC.PhysicalPath			%>
</textarea>

<textarea style="height:100px; width:30%;">
Saved:		[<%= CurrentPage.Created %>]
Updated:	[<%= CurrentPage.Updated %>]
Newed:		[<%= DataBinder.Eval(CurrentPage, "NewedDate") %>]
FileUrl:	[<%= CurrentPage.FileUrl %>]
MyItem:		[<%= CurrentPage.MyItem %>]
MyFile:		[<%= CurrentPage.MyFile %>]

CurrentCulture:	<%= System.Threading.Thread.CurrentThread.CurrentCulture.Name %>
Now:		<%= DateTime.Now %>
</textarea>

	<asp:ScriptManager runat="server" />
	<asp:UpdatePanel runat="server">
		<ContentTemplate>
			<asp:Button runat="server" Text="clickme" onclick="Unnamed3_Click" />
		</ContentTemplate>
	</asp:UpdatePanel>

	<a href="#" onclick="$('html').load('/ html');">RELOAD</a>

    <n2:DraggableToolbar runat="server" />
    
    <N2:EditableDisplay PropertyName="Text" runat="server" />
    
    <hr />
    
    <style>
        td{vertical-align:top;}
        .content{width:500px;}
    </style>
    
    <table><tr><td>
    <div style="border:dotted 1px blue;width:200px;clear:right;">
       <n2:DroppableZone id="Left" ZoneName="Left" runat="server" />
    </div>
    </td><td>

    <div class="content">
        <n2:DroppableZone ZoneName="Content" runat="server" />
    </div>
    </td><td>
    <div style="border:dotted 1px green;width:200px;clear:right;">
       <n2:DroppableZone id="MainDataContainer" ZoneName="Right" runat="server" Path="<%$ CurrentPage: ZonePath %>" />
    </div>

    </td></tr></table>

    
    
    <hr />
    
    
    <N2:ItemDataSource ID="ids" runat="server" />
    <asp:GridView ID="gvChildren" runat="server" DataSourceID="ids" AllowPaging="True" AllowSorting="True" AutoGenerateDeleteButton="True" AutoGenerateEditButton="True" AutoGenerateSelectButton="True" AutoGenerateColumns="false" DataKeyNames="ID">
        <EditRowStyle BackColor="#C0FFC0" />
        <SelectedRowStyle BackColor="#FFFFC0" />
        <Columns>
            <asp:HyperLinkField DataTextField="Title" DataNavigateUrlFields="Url" />
            <asp:BoundField DataField="Name" />
        </Columns>
    </asp:GridView>

</asp:Content>