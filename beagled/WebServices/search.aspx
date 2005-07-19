<%@ Page Inherits="WebService_CodeBehind.BeagleWebPage" ClassName="BeagleWebPage" %>
<html>
<head>
    <title>Beagle Search Web Form</title>
</head>
<body>
    <form id="SearchForm" runat="server">      
		<img src="local/share/pixmaps/dog.png" align="center" width="48" height="64">
		<asp:Label id="Label1" ForeColor="Purple" runat="server" text="<b> Beagle Search Page</b>"/>
		<br>
        <asp:Label id="SearchLabel" runat="server" text="Enter Search Terms "/>
        <asp:TextBox id="SearchBox" BackColor="LightCyan" Columns="20" runat="server"/>
	    <asp:DropDownList BackColor="LightCyan" id="sourceList" runat="server">
		   <asp:ListItem text="Anywhere" selected="true" value="Anywhere" />
		   <asp:ListItem text="in Files" value="Files" />
		   <asp:ListItem text="in AddressBook" value="Contact" />
		   <asp:ListItem text="in Mail" value="MailMessage" />
		   <asp:ListItem text="in WebPages" value="WebHistory" />
		   <asp:ListItem text="in Chats" value="IMLog" />
	    </asp:DropDownList>
	    <asp:CheckBox ForeColor="Maroon" id="GlobalSearchCheckBox" runat="server" Text="NetBeagleSearch "/>
        <asp:Button id="Search" onclick="Search_Click" runat="server" Text="FIND"/>
	    <p>
        <asp:Label id="Output" runat="server"/>
	    <p>
        <asp:Button id="Back" onclick="Back_Click" runat="server" Text="Show Previous Results" />
        <asp:Button id="Forward" onclick="Forward_Click" runat="server" Text="Show More Results" />
    </form>
</body>
</html>
