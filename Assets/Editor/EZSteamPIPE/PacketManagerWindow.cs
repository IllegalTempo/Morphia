using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Text;

public class PacketManagerWindow : EditorWindow
{
    private class PacketRow
    {
        public string Scope; // "ServerPackets" or "ClientPackets"
        public string Name;  // Enum member name
        public List<string> Senders = new List<string>();
        public List<string> Handlers = new List<string>();
        public string FullName => $"{Scope}.{Name}";
    }

    private Vector2 _scroll;
    private string _search = string.Empty;
    private List<PacketRow> _serverPacketRows = new List<PacketRow>(); // Packets sent by server, handled by client
    private List<PacketRow> _clientPacketRows = new List<PacketRow>(); // Packets sent by client, handled by server
    private double _lastParseTime;

    [MenuItem("Tools/Packet Manager")]
    public static void ShowWindow()
    {
        GetWindow<PacketManagerWindow>("Packet Manager");
    }

    void OnGUI()
    {
        GUILayout.Label("Packet Viewer", EditorStyles.boldLabel);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Refresh", GUILayout.Width(100)))
            {
                TryParseAll();
            }
            GUILayout.Space(8);
            if (GUILayout.Button("Add Packet", GUILayout.Width(100)))
            {
                AddPacketWindow.ShowWindow();
            }
            GUILayout.Space(8);
            _search = EditorGUILayout.TextField("Search", _search);
            GUILayout.FlexibleSpace();
        }

        if (_serverPacketRows.Count == 0 && _clientPacketRows.Count == 0)
        {
            if ((EditorApplication.timeSinceStartup - _lastParseTime) > 0.5f)
            {
                TryParseAll();
            }
        }

        _scroll = EditorGUILayout.BeginScrollView(_scroll);

        DrawSectionHeader("Server -> Client (ServerPackets)");
        DrawTableHeader();
        foreach (var row in Filter(_serverPacketRows))
        {
            DrawRow(row);
        }

        GUILayout.Space(12);

        DrawSectionHeader("Client -> Server (ClientPackets)");
        DrawTableHeader();
        foreach (var row in Filter(_clientPacketRows))
        {
            DrawRow(row);
        }

        EditorGUILayout.EndScrollView();
    }

    private IEnumerable<PacketRow> Filter(IEnumerable<PacketRow> rows)
    {
        if (string.IsNullOrWhiteSpace(_search)) return rows;
        var s = _search.Trim();
        return rows.Where(r => r.FullName.IndexOf(s, StringComparison.OrdinalIgnoreCase) >= 0
                            || r.Senders.Any(m => m.IndexOf(s, StringComparison.OrdinalIgnoreCase) >= 0)
                            || r.Handlers.Any(m => m.IndexOf(s, StringComparison.OrdinalIgnoreCase) >= 0));
    }

    private void DrawSectionHeader(string title)
    {
        GUILayout.Space(6);
        var rect = EditorGUILayout.GetControlRect(false, 22);
        EditorGUI.DrawRect(rect, new Color(0.18f, 0.18f, 0.18f, 1f));
        EditorGUI.LabelField(rect, title, new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleLeft });
        GUILayout.Space(4);
    }

    private void DrawTableHeader()
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Label("Packet", EditorStyles.miniBoldLabel, GUILayout.Width(260));
            GUILayout.Label("Send Method(s)", EditorStyles.miniBoldLabel, GUILayout.Width(320));
            GUILayout.Label("Handle Method(s)", EditorStyles.miniBoldLabel);
            GUILayout.Space(60); // Space for Remove button
        }
    }

    private void DrawRow(PacketRow row)
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Label(row.FullName, GUILayout.Width(260));
            GUILayout.Label(row.Senders.Count > 0 ? string.Join(", ", row.Senders.Distinct()) : "-", GUILayout.Width(320));
            GUILayout.Label(row.Handlers.Count > 0 ? string.Join(", ", row.Handlers.Distinct()) : "-");
            
            if (GUILayout.Button("Remove", GUILayout.Width(60)))
            {
                if (EditorUtility.DisplayDialog("Confirm Removal", 
                    "Are you sure you want to remove packet '" + row.Name + "'?\n\nThis will remove:\n" +
                    "- Enum entry\n" +
                    "- Send method(s)\n" +
                    "- Handle method(s)\n" +
                    "- Handler registration(s)", 
                    "Remove", "Cancel"))
                {
                    RemovePacketWindow.RemovePacket(row.Scope, row.Name);
                    TryParseAll();
                }
            }
        }
    }

    private void TryParseAll()
    {
        _lastParseTime = EditorApplication.timeSinceStartup;

        string packetSendText = ReadScriptTextByName("PacketSend");
        string gameClientText = ReadScriptTextByName("GameClient");
        string gameServerText = ReadScriptTextByName("GameServer");

        var serverSenders = new Dictionary<string, List<string>>(); // key: ServerPackets.<Name>
        var clientSenders = new Dictionary<string, List<string>>(); // key: ClientPackets.<Name>

        if (!string.IsNullOrEmpty(packetSendText))
        {
            ExtractSendersFromPacketSend(packetSendText, serverSenders, clientSenders);
        }

        var serverHandlers = new Dictionary<string, List<string>>(); // key: ServerPackets.<Name> -> Client handlers
        var clientHandlers = new Dictionary<string, List<string>>(); // key: ClientPackets.<Name> -> Server handlers

        if (!string.IsNullOrEmpty(gameClientText))
        {
            foreach (var pair in ExtractHandlerMapFromManager(gameClientText, isClient: true))
            {
                if (!serverHandlers.TryGetValue(pair.Key, out var list)) { list = new List<string>(); serverHandlers[pair.Key] = list; }
                list.Add(pair.Value);
            }
        }
        if (!string.IsNullOrEmpty(gameServerText))
        {
            foreach (var pair in ExtractHandlerMapFromManager(gameServerText, isClient: false))
            {
                if (!clientHandlers.TryGetValue(pair.Key, out var list)) { list = new List<string>(); clientHandlers[pair.Key] = list; }
                list.Add(pair.Value);
            }
        }

        _serverPacketRows = BuildRows("ServerPackets", GetEnumNamesSafe("ServerPackets"), serverSenders, serverHandlers);
        _clientPacketRows = BuildRows("ClientPackets", GetEnumNamesSafe("ClientPackets"), clientSenders, clientHandlers);
        Repaint();
    }

    private List<PacketRow> BuildRows(string scope, IEnumerable<string> enumNames,
        Dictionary<string, List<string>> senders,
        Dictionary<string, List<string>> handlers)
    {
        var rows = new List<PacketRow>();
        foreach (var name in enumNames)
        {
            var key = $"{scope}.{name}";
            var row = new PacketRow { Scope = scope, Name = name };
            if (senders.TryGetValue(key, out var s)) row.Senders.AddRange(s);
            if (handlers.TryGetValue(key, out var h)) row.Handlers.AddRange(h);
            rows.Add(row);
        }
        // Also include any packets that were found in parsing but not present in enum reflection (fallback)
        foreach (var extra in senders.Keys.Concat(handlers.Keys))
        {
            var parts = extra.Split('.');
            if (parts.Length != 2) continue;
            if (parts[0] != scope) continue;
            if (rows.Any(r => r.Name == parts[1])) continue;
            var row = new PacketRow { Scope = parts[0], Name = parts[1] };
            if (senders.TryGetValue(extra, out var s)) row.Senders.AddRange(s);
            if (handlers.TryGetValue(extra, out var h)) row.Handlers.AddRange(h);
            rows.Add(row);
        }
        rows.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
        return rows;
    }

    private static IEnumerable<string> GetEnumNamesSafe(string nestedEnumName)
    {
        try
        {
            var enumType = typeof(PacketSend).GetNestedType(nestedEnumName, BindingFlags.Public);
            if (enumType != null && enumType.IsEnum)
            {
                return Enum.GetNames(enumType);
            }
        }
        catch { }
        return Enumerable.Empty<string>();
    }

    private static void ExtractSendersFromPacketSend(string packetSendText,
        Dictionary<string, List<string>> serverSenders,
        Dictionary<string, List<string>> clientSenders)
    {
        // Find all "new packet((int)ServerPackets.XYZ)" and map to the containing method name
        var rxPacketNew = new Regex(@"new\s+packet\s*\(\s*\(int\)\s*(?<scope>ServerPackets|ClientPackets)\s*\.\s*(?<name>\w+)\s*\)", RegexOptions.Multiline);
        var rxMethod = new Regex(@"public\s+static\s+Result\s+(?<method>\w+)\s*\(", RegexOptions.Multiline);

        var methodMatches = rxMethod.Matches(packetSendText).Cast<Match>().ToList();

        foreach (Match m in rxPacketNew.Matches(packetSendText))
        {
            var scope = m.Groups["scope"].Value;
            var name = m.Groups["name"].Value;
            var idx = m.Index;
            // Find nearest previous method declaration
            string methodName = null;
            for (int i = methodMatches.Count - 1; i >= 0; i--)
            {
                if (methodMatches[i].Index <= idx)
                {
                    methodName = methodMatches[i].Groups["method"].Value;
                    break;
                }
            }
            if (string.IsNullOrEmpty(methodName)) continue;

            var key = $"{scope}.{name}";
            var dict = scope == "ServerPackets" ? serverSenders : clientSenders;
            if (!dict.TryGetValue(key, out var list)) { list = new List<string>(); dict[key] = list; }
            if (!list.Contains(methodName)) list.Add(methodName);
        }
    }

    private static IEnumerable<KeyValuePair<string, string>> ExtractHandlerMapFromManager(string managerText, bool isClient)
    {
        // Extract the initializer content of the dictionary in GameClient or GameServer
        // GameClient: ClientPacketHandles maps PacketSend.ServerPackets.X -> PacketHandles_Method.Client_Handle_*
        // GameServer: ServerPacketHandles maps PacketSend.ClientPackets.X -> PacketHandles_Method.Server_Handle_*
        var dictName = isClient ? "ClientPacketHandles" : "ServerPacketHandles";
        int dictIndex = managerText.IndexOf(dictName, StringComparison.Ordinal);
        if (dictIndex < 0) yield break;

        int newDictIndex = managerText.IndexOf("new Dictionary", dictIndex, StringComparison.Ordinal);
        if (newDictIndex < 0) yield break;

        int braceStart = managerText.IndexOf('{', newDictIndex);
        if (braceStart < 0) yield break;

        // Find matching closing brace for the initializer
        int depth = 0;
        int i = braceStart;
        for (; i < managerText.Length; i++)
        {
            char c = managerText[i];
            if (c == '{') depth++;
            else if (c == '}') { depth--; if (depth == 0) { i++; break; } }
        }
        if (depth != 0) yield break;

        string content = managerText.Substring(braceStart, i - braceStart);

        var rxEntry = new Regex(@"\{\s*\(int\)\s*PacketSend\s*\.\s*(?<scope>ClientPackets|ServerPackets)\s*\.\s*(?<name>\w+)\s*,\s*PacketHandles_Method\s*\.\s*(?<handler>\w+)\s*\}", RegexOptions.Multiline);
        foreach (Match m in rxEntry.Matches(content))
        {
            var scope = m.Groups["scope"].Value;
            var name = m.Groups["name"].Value;
            var handler = m.Groups["handler"].Value;
            yield return new KeyValuePair<string, string>($"{scope}.{name}", handler);
        }
    }

    private static string ReadScriptTextByName(string className)
    {
        // Prefer AssetDatabase search to be path-agnostic
        var guids = AssetDatabase.FindAssets(className + " t:Script");
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.EndsWith(className + ".cs", StringComparison.OrdinalIgnoreCase)) continue;
            var fullPath = ToFullPath(path);
            try
            {
                return File.ReadAllText(fullPath);
            }
            catch { }
        }
        return string.Empty;
    }

    private static string ToFullPath(string assetRelativePath)
    {
        var projectRoot = Directory.GetParent(Application.dataPath).FullName;
        return Path.Combine(projectRoot, assetRelativePath.Replace('/', Path.DirectorySeparatorChar));
    }
}

public class AddPacketWindow : EditorWindow
{
    private string _packetName = "";
    private int _packetTypeIndex = 0;
    private readonly string[] _packetTypes = { "ServerPackets (Server -> Client)", "ClientPackets (Client -> Server)" };
    private bool _generateSendMethod = true;
    private bool _generateHandleMethod = true;
    private bool _registerHandler = true;

    public static void ShowWindow()
    {
        var window = GetWindow<AddPacketWindow>("Add New Packet");
        window.minSize = new Vector2(400, 250);
        window.maxSize = new Vector2(400, 250);
    }

    void OnGUI()
    {
        GUILayout.Label("Add New Packet", EditorStyles.boldLabel);
        GUILayout.Space(10);

        _packetName = EditorGUILayout.TextField("Packet Name:", _packetName);
        GUILayout.Space(5);
        
        _packetTypeIndex = EditorGUILayout.Popup("Packet Type:", _packetTypeIndex, _packetTypes);
        GUILayout.Space(10);

        GUILayout.Label("Options:", EditorStyles.boldLabel);
        _generateSendMethod = EditorGUILayout.Toggle("Generate Send Method Template", _generateSendMethod);
        _generateHandleMethod = EditorGUILayout.Toggle("Generate Handle Method Template", _generateHandleMethod);
        _registerHandler = EditorGUILayout.Toggle("Register Handler in Manager", _registerHandler);
        
        GUILayout.Space(20);

        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.FlexibleSpace();
            
            if (GUILayout.Button("Cancel", GUILayout.Width(100)))
            {
                Close();
            }

            GUI.enabled = !string.IsNullOrWhiteSpace(_packetName);
            if (GUILayout.Button("Add Packet", GUILayout.Width(100)))
            {
                AddPacket();
            }
            GUI.enabled = true;
        }
    }

    private void AddPacket()
    {
        if (string.IsNullOrWhiteSpace(_packetName))
        {
            EditorUtility.DisplayDialog("Error", "Packet name cannot be empty.", "OK");
            return;
        }

        _packetName = _packetName.Trim();
        if (!Regex.IsMatch(_packetName, @"^[a-zA-Z_][a-zA-Z0-9_]*$"))
        {
            EditorUtility.DisplayDialog("Error", "Packet name must be a valid C# identifier.", "OK");
            return;
        }

        bool isServerPacket = _packetTypeIndex == 0;
        string enumType = isServerPacket ? "ServerPackets" : "ClientPackets";
        string prefix = isServerPacket ? "Server" : "Client";

        try
        {
            if (!AddPacketToEnum(enumType))
            {
                return;
            }

            if (_generateSendMethod)
            {
                GenerateSendMethod(prefix, isServerPacket);
            }

            if (_generateHandleMethod)
            {
                GenerateHandleMethod(prefix, isServerPacket);
            }

            if (_registerHandler && _generateHandleMethod)
            {
                RegisterHandler(isServerPacket);
            }

            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Success", "Packet '" + _packetName + "' added successfully!", "OK");
            Close();
        }
        catch (Exception ex)
        {
            EditorUtility.DisplayDialog("Error", "Failed to add packet: " + ex.Message, "OK");
        }
    }

    private bool AddPacketToEnum(string enumType)
    {
        var guids = AssetDatabase.FindAssets("PacketSend t:Script");
        string filePath = null;
        
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.EndsWith("PacketSend.cs", StringComparison.OrdinalIgnoreCase))
            {
                filePath = ToFullPath(path);
                break;
            }
        }

        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
        {
            EditorUtility.DisplayDialog("Error", "Could not find PacketSend.cs file.", "OK");
            return false;
        }

        string content = File.ReadAllText(filePath);
        
        // More precise regex to match the enum including the closing brace
        var enumPattern = @"public\s+enum\s+" + enumType + @"\s*\{([^}]*)\}";
        var match = Regex.Match(content, enumPattern, RegexOptions.Singleline);
        
        if (!match.Success)
        {
            EditorUtility.DisplayDialog("Error", "Could not find " + enumType + " enum in PacketSend.cs", "OK");
            return false;
        }

        string enumContent = match.Groups[1].Value;
        if (Regex.IsMatch(enumContent, @"\b" + _packetName + @"\b"))
        {
            EditorUtility.DisplayDialog("Error", "Packet '" + _packetName + "' already exists in " + enumType + ".", "OK");
            return false;
        }

        // Find the highest enum value
        var valueMatches = Regex.Matches(enumContent, @"=\s*(\d+)");
        int maxValue = -1;
        foreach (Match m in valueMatches)
        {
            if (int.TryParse(m.Groups[1].Value, out int val) && val > maxValue)
            {
                maxValue = val;
            }
        }
        int newValue = maxValue + 1;

        // Find the position just before the closing brace of the enum
        // We need to trim whitespace and check if we need a comma
        string trimmedEnum = enumContent.TrimEnd();
        bool needsComma = trimmedEnum.Length > 0 && !trimmedEnum.EndsWith(",");
        
        // Build the insertion text
        string insertText = (needsComma ? "," : "") + "\n        " + _packetName + " = " + newValue;
        
        // Calculate the position to insert (right before the closing brace)
        int enumStartPos = match.Groups[1].Index;
        int enumEndPos = enumStartPos + match.Groups[1].Length;
        
        // Insert the new entry
        content = content.Substring(0, enumEndPos) + insertText + "\n    " + content.Substring(enumEndPos);
        
        File.WriteAllText(filePath, content);
        return true;
    }

    private void GenerateSendMethod(string prefix, bool isServerPacket)
    {
        var guids = AssetDatabase.FindAssets("PacketSend t:Script");
        string filePath = null;
        
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.EndsWith("PacketSend.cs", StringComparison.OrdinalIgnoreCase))
            {
                filePath = ToFullPath(path);
                break;
            }
        }

        if (string.IsNullOrEmpty(filePath)) return;

        string content = File.ReadAllText(filePath);
        string enumType = isServerPacket ? "ServerPackets" : "ClientPackets";
        string methodName = prefix + "_Send_" + _packetName;

        if (content.Contains("public static Result " + methodName + "("))
        {
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine();
        sb.AppendLine("    ");
        if (isServerPacket)
        {
            sb.AppendLine("    public static Result " + methodName + "(NetworkPlayer target)");
            sb.AppendLine("    {");
            sb.AppendLine("        using (packet p = new packet((int)ServerPackets." + _packetName + "))");
            sb.AppendLine("        {");
            sb.AppendLine("            // TODO: Write packet data here");
            sb.AppendLine("            // p.Write(...);");
            sb.AppendLine("            ");
            sb.AppendLine("            return target.SendPacket(p);");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
        }
        else
        {
            sb.AppendLine("    public static Result " + methodName + "()");
            sb.AppendLine("    {");
            sb.AppendLine("        using (packet p = new packet((int)ClientPackets." + _packetName + "))");
            sb.AppendLine("        {");
            sb.AppendLine("            // TODO: Write packet data here");
            sb.AppendLine("            // p.Write(...);");
            sb.AppendLine("            ");
            sb.AppendLine("            return SendToServer(p);");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
        }

        // For ServerPackets: insert before "//Area for client Packets!"
        // For ClientPackets: insert before "private static Result SendToServer" method
        string sectionMarker;
        if (isServerPacket)
        {
            sectionMarker = "//Area for client Packets!";
        }
        else
        {
            // Look for the SendToServer method - insert before it
            sectionMarker = "private static Result SendToServer(packet p)";
        }
        
        int insertIndex = content.IndexOf(sectionMarker, StringComparison.Ordinal);
        
        if (insertIndex > 0)
        {
            content = content.Insert(insertIndex, sb.ToString());
            File.WriteAllText(filePath, content);
        }
        else
        {
            Debug.LogWarning("Could not find insertion point: " + sectionMarker);
        }
    }

    private void GenerateHandleMethod(string prefix, bool isServerPacket)
    {
        var guids = AssetDatabase.FindAssets("PacketHandles t:Script");
        string filePath = null;
        
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.EndsWith("PacketHandles.cs", StringComparison.OrdinalIgnoreCase))
            {
                filePath = ToFullPath(path);
                break;
            }
        }

        if (string.IsNullOrEmpty(filePath)) return;

        string content = File.ReadAllText(filePath);
        string handlePrefix = isServerPacket ? "Client" : "Server";
        string methodName = handlePrefix + "_Handle_" + _packetName;

        if (content.Contains("public static void " + methodName + "("))
        {
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine();
        if (isServerPacket)
        {
            sb.AppendLine("\tpublic static void " + methodName + "(Connection c, packet packet)");
            sb.AppendLine("\t{");
            sb.AppendLine("\t\t// TODO: Read packet data here");
            sb.AppendLine("\t\t// var data = packet.Read...();");
            sb.AppendLine("\t\t");
            sb.AppendLine("\t\t// TODO: Handle the packet");
            sb.AppendLine("\t}");
        }
        else
        {
            sb.AppendLine("\tpublic static void " + methodName + "(NetworkPlayer p, packet packet)");
            sb.AppendLine("\t{");
            sb.AppendLine("\t\t// TODO: Read packet data here");
            sb.AppendLine("\t\t// var data = packet.Read...();");
            sb.AppendLine("\t\t");
            sb.AppendLine("\t\t// TODO: Handle the packet");
            sb.AppendLine("\t}");
        }

        // Find the last closing brace of the class
        int lastBraceIndex = content.LastIndexOf('}');
        if (lastBraceIndex > 0)
        {
            // Insert before the last closing brace
            content = content.Insert(lastBraceIndex, sb.ToString());
            File.WriteAllText(filePath, content);
        }
    }

    private void RegisterHandler(bool isServerPacket)
    {
        // Determine which manager to update based on who RECEIVES the packet
        // ServerPackets are sent BY server and received BY client -> register in GameClient
        // ClientPackets are sent BY client and received BY server -> register in GameServer
        string className = isServerPacket ? "GameClient" : "GameServer";
        var guids = AssetDatabase.FindAssets(className + " t:Script");
        string filePath = null;
        
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.EndsWith(className + ".cs", StringComparison.OrdinalIgnoreCase))
            {
                filePath = ToFullPath(path);
                break;
            }
        }

        if (string.IsNullOrEmpty(filePath)) 
        {
            Debug.LogWarning("Could not find " + className + ".cs file");
            return;
        }

        string content = File.ReadAllText(filePath);
        string dictName = isServerPacket ? "ClientPacketHandles" : "ServerPacketHandles";
        string enumType = isServerPacket ? "ServerPackets" : "ClientPackets";
        string handlePrefix = isServerPacket ? "Client" : "Server";
        string methodName = handlePrefix + "_Handle_" + _packetName;

        // Find the dictionary initialization by looking for the pattern and matching braces
        int dictStart = content.IndexOf(dictName + " = new Dictionary");
        if (dictStart < 0)
        {
            Debug.LogWarning("Could not find " + dictName + " dictionary initialization");
            return;
        }

        // Find the opening brace of the dictionary initializer
        int braceStart = content.IndexOf('{', dictStart);
        if (braceStart < 0) return;

        // Find the matching closing brace
        int depth = 0;
        int braceEnd = -1;
        for (int i = braceStart; i < content.Length; i++)
        {
            if (content[i] == '{') depth++;
            else if (content[i] == '}')
            {
                depth--;
                if (depth == 0)
                {
                    braceEnd = i;
                    break;
                }
            }
        }

        if (braceEnd < 0)
        {
            Debug.LogWarning("Could not find closing brace for " + dictName);
            return;
        }

        // Get the content between braces
        string dictContent = content.Substring(braceStart + 1, braceEnd - braceStart - 1);
        
        // Check if there are existing entries
        bool hasEntries = dictContent.Trim().Length > 0 && dictContent.Contains("(int)PacketSend.");
        
        // Build the new entry
        string newEntry = (hasEntries ? "," : "") + "\n            { (int)PacketSend." + enumType + "." + _packetName + ", PacketHandles_Method." + methodName + " }";
        
        // Insert before the closing brace
        content = content.Substring(0, braceEnd) + newEntry + "\n        " + content.Substring(braceEnd);
        
        File.WriteAllText(filePath, content);
        Debug.Log("Registered handler for " + _packetName + " in " + className);
    }

    private static string ToFullPath(string assetRelativePath)
    {
        var projectRoot = Directory.GetParent(Application.dataPath).FullName;
        return Path.Combine(projectRoot, assetRelativePath.Replace('/', Path.DirectorySeparatorChar));
    }
}

public class RemovePacketWindow
{
    public static void RemovePacket(string scope, string packetName)
    {
        bool isServerPacket = scope == "ServerPackets";
        
        try
        {
            bool anyRemoved = false;
            
            // Remove from enum
            if (RemoveFromEnum(scope, packetName))
            {
                anyRemoved = true;
            }
            
            // Remove send methods
            if (RemoveSendMethods(packetName, isServerPacket))
            {
                anyRemoved = true;
            }
            
            // Remove handle methods
            if (RemoveHandleMethods(packetName, isServerPacket))
            {
                anyRemoved = true;
            }
            
            // Remove handler registration
            if (UnregisterHandler(packetName, isServerPacket))
            {
                anyRemoved = true;
            }
            
            if (anyRemoved)
            {
                AssetDatabase.Refresh();
                EditorUtility.DisplayDialog("Success", "Packet '" + packetName + "' removed successfully!", "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Info", "No references to packet '" + packetName + "' were found.", "OK");
            }
        }
        catch (Exception ex)
        {
            EditorUtility.DisplayDialog("Error", "Failed to remove packet: " + ex.Message, "OK");
        }
    }
    
    private static bool RemoveFromEnum(string enumType, string packetName)
    {
        var guids = AssetDatabase.FindAssets("PacketSend t:Script");
        string filePath = null;
        
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.EndsWith("PacketSend.cs", StringComparison.OrdinalIgnoreCase))
            {
                filePath = ToFullPath(path);
                break;
            }
        }
        
        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
        {
            return false;
        }
        
        string content = File.ReadAllText(filePath);
        
        // Find and remove the enum entry (handles with or without trailing comma)
        var entryPattern = @",?\s*" + packetName + @"\s*=\s*\d+\s*,?";
        var match = Regex.Match(content, entryPattern);
        
        if (match.Success)
        {
            // Remove the entry
            string before = content.Substring(0, match.Index);
            string after = content.Substring(match.Index + match.Length);
            
            // Clean up double commas or trailing commas before closing brace
            content = before + after;
            content = Regex.Replace(content, @",\s*,", ",");
            content = Regex.Replace(content, @",(\s*)\}", "$1}");
            
            File.WriteAllText(filePath, content);
            return true;
        }
        
        return false;
    }
    
    private static bool RemoveSendMethods(string packetName, bool isServerPacket)
    {
        var guids = AssetDatabase.FindAssets("PacketSend t:Script");
        string filePath = null;
        
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.EndsWith("PacketSend.cs", StringComparison.OrdinalIgnoreCase))
            {
                filePath = ToFullPath(path);
                break;
            }
        }
        
        if (string.IsNullOrEmpty(filePath)) return false;
        
        string content = File.ReadAllText(filePath);
        bool removed = false;
        
        // Pattern to match the entire method that references this packet
        string prefix = isServerPacket ? "Server" : "Client";
        string enumType = isServerPacket ? "ServerPackets" : "ClientPackets";
        
        // Find methods that create packets with this packet type
        var methodPattern = @"public\s+static\s+Result\s+\w+\s*\([^)]*\)\s*\{[^}]*new\s+packet\s*\(\s*\(int\)\s*" + 
                           enumType + @"\." + packetName + @"[^}]*\}\s*;?\s*\}";
        
        var matches = Regex.Matches(content, methodPattern, RegexOptions.Singleline);
        
        // Remove matches in reverse order to preserve indices
        for (int i = matches.Count - 1; i >= 0; i--)
        {
            var match = matches[i];
            // Remove the method and any preceding empty lines
            int startIndex = match.Index;
            while (startIndex > 0 && (content[startIndex - 1] == '\n' || content[startIndex - 1] == '\r' || content[startIndex - 1] == ' ' || content[startIndex - 1] == '\t'))
            {
                startIndex--;
            }
            
            content = content.Remove(startIndex, match.Index + match.Length - startIndex);
            removed = true;
        }
        
        if (removed)
        {
            File.WriteAllText(filePath, content);
        }
        
        return removed;
    }
    
    private static bool RemoveHandleMethods(string packetName, bool isServerPacket)
    {
        var guids = AssetDatabase.FindAssets("PacketHandles t:Script");
        string filePath = null;
        
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.EndsWith("PacketHandles.cs", StringComparison.OrdinalIgnoreCase))
            {
                filePath = ToFullPath(path);
                break;
            }
        }
        
        if (string.IsNullOrEmpty(filePath)) return false;
        
        string content = File.ReadAllText(filePath);
        string handlePrefix = isServerPacket ? "Client" : "Server";
        string methodName = handlePrefix + "_Handle_" + packetName;
        
        // Pattern to match the entire handler method
        var methodPattern = @"public\s+static\s+(async\s+)?void\s+" + methodName + @"\s*\([^)]*\)\s*\{";
        var match = Regex.Match(content, methodPattern);
        
        if (match.Success)
        {
            // Find the matching closing brace
            int braceCount = 1;
            int searchIndex = match.Index + match.Length;
            int endIndex = -1;
            
            while (searchIndex < content.Length && braceCount > 0)
            {
                if (content[searchIndex] == '{') braceCount++;
                else if (content[searchIndex] == '}') 
                {
                    braceCount--;
                    if (braceCount == 0)
                    {
                        endIndex = searchIndex + 1;
                        break;
                    }
                }
                searchIndex++;
            }
            
            if (endIndex > 0)
            {
                // Remove preceding whitespace/newlines
                int startIndex = match.Index;
                while (startIndex > 0 && (content[startIndex - 1] == '\n' || content[startIndex - 1] == '\r' || content[startIndex - 1] == '\t'))
                {
                    startIndex--;
                }
                
                content = content.Remove(startIndex, endIndex - startIndex);
                File.WriteAllText(filePath, content);
                return true;
            }
        }
        
        return false;
    }
    
    private static bool UnregisterHandler(string packetName, bool isServerPacket)
    {
        // Determine which manager to update based on who RECEIVES the packet
        // ServerPackets are sent BY server and received BY client -> registered in GameClient
        // ClientPackets are sent BY client and received BY server -> registered in GameServer
        string className = isServerPacket ? "GameClient" : "GameServer";
        var guids = AssetDatabase.FindAssets(className + " t:Script");
        string filePath = null;
        
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.EndsWith(className + ".cs", StringComparison.OrdinalIgnoreCase))
            {
                filePath = ToFullPath(path);
                break;
            }
        }
        
        if (string.IsNullOrEmpty(filePath)) return false;
        
        string content = File.ReadAllText(filePath);
        string enumType = isServerPacket ? "ServerPackets" : "ClientPackets";
        string handlePrefix = isServerPacket ? "Client" : "Server";
        string methodName = handlePrefix + "_Handle_" + packetName;
        
        // Pattern to match the dictionary entry (with or without trailing comma)
        var entryPattern = @"\{\s*\(int\)\s*PacketSend\." + enumType + @"\." + packetName + 
                          @"\s*,\s*PacketHandles_Method\." + methodName + @"\s*\}\s*,?";
        
        var match = Regex.Match(content, entryPattern);
        
        if (match.Success)
        {
            // Get the text before and after the match
            string before = content.Substring(0, match.Index);
            string after = content.Substring(match.Index + match.Length);
            
            // Check if we need to remove a preceding comma
            bool removePrecedingComma = before.TrimEnd().EndsWith(",");
            if (removePrecedingComma && !after.TrimStart().StartsWith("}"))
            {
                // Don't remove the comma if this isn't the last entry
                removePrecedingComma = false;
            }
            else if (removePrecedingComma)
            {
                // Remove trailing comma from before
                before = before.TrimEnd();
                before = before.Substring(0, before.Length - 1) + "\n            ";
            }
            
            content = before + after;
            
            // Clean up any double commas that might have been created
            content = Regex.Replace(content, @",\s*,", ",");
            
            File.WriteAllText(filePath, content);
            return true;
        }
        
        return false;
    }
    
    private static string ToFullPath(string assetRelativePath)
    {
        var projectRoot = Directory.GetParent(Application.dataPath).FullName;
        return Path.Combine(projectRoot, assetRelativePath.Replace('/', Path.DirectorySeparatorChar));
    }
}
