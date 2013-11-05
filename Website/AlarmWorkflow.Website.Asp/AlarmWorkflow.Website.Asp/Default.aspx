<%@ Page Title="AlarmWorkflow" Language="C#" MasterPageFile="~/Site.master" AutoEventWireup="true"
    CodeBehind="Default.aspx.cs" Inherits="AlarmWorkflow.Website.Asp.Default" %>

<asp:Content ID="HeaderContent" runat="server" ContentPlaceHolderID="HeadContent">
    <script type="text/javascript" src="https://maps.googleapis.com/maps/api/js?sensor=true"> </script>
    <script type="text/javascript" src="http://www.openlayers.org/api/OpenLayers.js"> </script>
    <script type="text/javascript" src="http://www.openstreetmap.org/openlayers/OpenStreetMap.js"> </script>
    <script type="text/javascript"><%= JSScripts %></script>
</asp:Content>
<asp:Content ID="BodyContent" runat="server" ContentPlaceHolderID="MainContent">
    <p>
        <table style="height: 100%; width: 100%">
            <tr style="height: 100%; width: 100%">
                <td style="width: 95%; height: 100%;">
                    <asp:Table ID="OperationTable" runat="server" Height="100%" BorderColor="#333333"
                        BorderStyle="Solid" BorderWidth="1px" GridLines="Both" Font-Size="45px" HorizontalAlign="Left"
                        Style="font-family: Arial">
                        <asp:TableRow ID="trInformation" runat="server" HorizontalAlign="Center" VerticalAlign="Middle">
                            <asp:TableCell ID="tcSchlagwort" runat="server" RowSpan="2" Font-Overline="False">
                                <asp:Label ID="lbSchlagwort" Font-Size="55px" Font-Bold="True" runat="server" ClientIDMode="Inherit"></asp:Label>
                            </asp:TableCell>
                            <asp:TableCell ID="tcObjekt" runat="server" Width="40%">
                                <asp:Label ID="lbObjekt" runat="server"></asp:Label>
                            </asp:TableCell>
                            <asp:TableCell ID="tcFZm" runat="server" RowSpan="5"></asp:TableCell>
                        </asp:TableRow>
                        <asp:TableRow ID="TableRow1" runat="server" HorizontalAlign="Center" VerticalAlign="Middle">
                            <asp:TableCell ID="tcBemerkung" runat="server">
                                <asp:Label ID="lbBemerkung" runat="server" Font-Bold="True"></asp:Label>
                            </asp:TableCell></asp:TableRow>
                        <asp:TableRow ID="trLocation" runat="server" HorizontalAlign="Center" VerticalAlign="Middle">
                            <asp:TableCell ID="tcAddress" runat="server">
                                <asp:Label ID="lbAddress" runat="server"></asp:Label>
                            </asp:TableCell>
                            <asp:TableCell ID="tcOrt" runat="server">
                                <asp:Label ID="lbOrt" runat="server"></asp:Label>
                            </asp:TableCell>
                        </asp:TableRow>
                        <asp:TableRow ID="trMap" Height="50%" runat="server" HorizontalAlign="Center" VerticalAlign="Middle">
                            <asp:TableCell ID="tcGoogle" runat="server">
                <div id="googlemap"  style="height: 100%; width: 100%;">
                </div>
                            </asp:TableCell>
                            <asp:TableCell ID="tcOSM" runat="server">
                <div id="osmmap"  style="height: 100%; width: 100%;">
                    <script type="text/javascript"><%= OSMCode %></script>
                </div>
                            </asp:TableCell>
                        </asp:TableRow>
                        <asp:TableRow ID="trResources" runat="server" HorizontalAlign="Center" VerticalAlign="Middle">
                            <asp:TableCell ID="tcResources" runat="server" ColumnSpan="2" HorizontalAlign="Center"
                                VerticalAlign="Middle">
                                <asp:Label ID="lbResources" runat="server"></asp:Label>
                            </asp:TableCell>
                        </asp:TableRow>
                    </asp:Table>
                </td>
                <td>
                    <asp:Table ID="FZ_Info" runat="server" BackColor="Silver" BorderColor="#333333" BorderWidth="1px"
                        Font-Bold="True" Font-Size="24pt" ForeColor="#999999" GridLines="Both" Height="100%"
                        HorizontalAlign="Right" Style="font-family: Arial" Width="100%">
                        <asp:TableRow ID="TableRow2" runat="server" ForeColor="Black" Height="20px">
                            <asp:TableCell ID="tclbPrio" runat="server" BorderStyle="None" Font-Bold="True" Font-Size="18pt"
                                HorizontalAlign="Center" Height="18">
                                <asp:Label ID="lblPrio" runat="server" Font-Bold="True">Prio</asp:Label>
                            </asp:TableCell>
                        </asp:TableRow>
                        <asp:TableRow ID="TableRow3" runat="server" ForeColor="Black" Height="32px">
                            <asp:TableCell ID="tcPrio" runat="server" BorderStyle="None" Font-Bold="True" Font-Size="50pt"
                                HorizontalAlign="Center" VerticalAlign="Middle">
                                <asp:Label ID="lbPrio" runat="server" Font-Bold="True"></asp:Label>
                            </asp:TableCell>
                        </asp:TableRow>
                        <asp:TableRow ID="TableRow4" runat="server">
                            <asp:TableCell ID="tcFZ1" runat="server" HorizontalAlign="Center" VerticalAlign="Middle">
                                <asp:Label ID="lbFZ1" runat="server" Font-Bold="True"></asp:Label>
                            </asp:TableCell>
                        </asp:TableRow>
                        <asp:TableRow ID="TableRow5" runat="server">
                            <asp:TableCell ID="tcFZ2" runat="server" HorizontalAlign="Center" VerticalAlign="Middle">
                                <asp:Label ID="lbFZ2" runat="server" Font-Bold="True"></asp:Label>
                            </asp:TableCell>
                        </asp:TableRow>
                        <asp:TableRow ID="TableRow6" runat="server">
                            <asp:TableCell ID="tcFZ3" runat="server" HorizontalAlign="Center" VerticalAlign="Middle">
                                <asp:Label ID="lbFZ3" runat="server" Font-Bold="True"></asp:Label>
                            </asp:TableCell>
                        </asp:TableRow>
                        <asp:TableRow ID="TableRow7" runat="server">
                            <asp:TableCell ID="tcFZ4" runat="server" HorizontalAlign="Center" VerticalAlign="Middle">
                                <asp:Label ID="lbFZ4" runat="server" Font-Bold="True"></asp:Label>
                            </asp:TableCell>
                        </asp:TableRow>
                        <asp:TableRow ID="TableRow8" runat="server">
                            <asp:TableCell ID="tcFZ5" runat="server" HorizontalAlign="Center" VerticalAlign="Middle">
                                <asp:Label ID="lbFZ5" runat="server" Font-Bold="True"></asp:Label>
                            </asp:TableCell>
                        </asp:TableRow>
                        <asp:TableRow ID="TableRow9" runat="server">
                            <asp:TableCell ID="tcFZ6" runat="server" HorizontalAlign="Center" VerticalAlign="Middle">
                                <asp:Label ID="lbFZ6" runat="server" Font-Bold="True"></asp:Label>
                            </asp:TableCell>
                        </asp:TableRow>
                        <asp:TableRow ID="TableRow10" runat="server">
                            <asp:TableCell ID="tcFZ7" runat="server" HorizontalAlign="Center" VerticalAlign="Middle">
                                <asp:Label ID="lbFZ7" runat="server" Font-Bold="True"></asp:Label>
                            </asp:TableCell>
                        </asp:TableRow>
                        <asp:TableRow ID="TableRow11" runat="server">
                            <asp:TableCell ID="tcFZ8" runat="server" HorizontalAlign="Center" VerticalAlign="Middle">
                                <asp:Label ID="lbFZ8" runat="server" Font-Bold="True"></asp:Label>
                            </asp:TableCell>
                        </asp:TableRow>
                        <asp:TableRow ID="TableRow12" runat="server" HorizontalAlign="Center" VerticalAlign="Middle">
                            <asp:TableCell ID="tcTimeLeft" runat="server" HorizontalAlign="Center" VerticalAlign="Middle"
                                Height="16" Font-Bold="True">
                                <asp:Label ID="lbTimeLeft" runat="server" Font-Bold="False"></asp:Label>
                            </asp:TableCell>
                        </asp:TableRow>
                    </asp:Table>
                </td>
            </tr>
        </table>
        <asp:Label Font-Size="15px" ID="DebugLabel" runat="server" Text="DebugInformation" />
        <asp:LinkButton Style="padding-left: 5px" ID="ResetButton" runat="server" OnClick="ResetButton_Click">Reset</asp:LinkButton>
    </p>
    <asp:ScriptManager ID="_ScriptManager" runat="server" />
    <asp:Timer runat="server" ID="_UpdateTimer" OnTick="UpdateTimer_Tick" Interval="10000" />
    <asp:UpdatePanel runat="server" ID="_TimedPanel" UpdateMode="Conditional">
        <Triggers>
            <asp:AsyncPostBackTrigger ControlID="_UpdateTimer" EventName="Tick" />
        </Triggers>
        <ContentTemplate>
        </ContentTemplate>
    </asp:UpdatePanel>
</asp:Content>
