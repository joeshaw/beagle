<%@ Page Inherits="WebService_CodeBehind.BeagleWebPage" ClassName="BeagleWebPage" %>
<html>
<head>
    <title>Beagle Search Web Form</title>
</head>
<body>

<img src="file:///usr/local/share/doc/xsp/test/beagle/dog.png" align="center" width="55" height="55"><asp:Label id="Label1" runat="server" text="<b>Beagle Search Page</b>"></asp:Label>

    <form id="SearchForm" runat="server">  
        <asp:Label id="SearchLabel" runat="server" text="Enter Beagle Search Terms "/>
        <asp:TextBox id="SearchBox" runat="server"/>
	    <asp:DropDownList id="sourceList" runat="server">
		   <asp:ListItem text="Anywhere" selected="true" value="Anywhere" />
		   <asp:ListItem text="in Files" value="Files" />
		   <asp:ListItem text="in AddressBook" value="Contact" />
		   <asp:ListItem text="in Mail" value="MailMessage" />
		   <asp:ListItem text="in WebPages" value="WebHistory" />
		   <asp:ListItem text="in Chats" value="IMLog" />
	    </asp:DropDownList>
        <asp:Button id="Search" onclick="Search_Click" runat="server" Text="Find"/>
	    <p>
        <asp:Label id="Output" runat="server"/>
	    <p>
        <asp:Button id="Back" onclick="Back_Click" runat="server" Text="Show Previous Results" />
        <asp:Button id="Forward" onclick="Forward_Click" runat="server" Text="Show More Results" />
    </form>
</body>
</html>
