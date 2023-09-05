# ------------------------------------------------------------------------
# Copyright 2023 Essigstudios Austria / Kaiser A.
#
# Licensed under the Apache License, Version 2.0 (the "License");
# you may not use this file except in compliance with the License.
# You may obtain a copy of the License at
# 
#     http://www.apache.org/licenses/LICENSE-2.0

# Unless required by applicable law or agreed to in writing, software
# distributed under the License is distributed on an "AS IS" BASIS,
# WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
# See the License for the specific language governing permissions and
# limitations under the License.
# ------------------------------------------------------------------------

extends Node

var m_ServerPath
var m_PlayerName

var m_LastChatHash
var m_LastAssetHash
var m_LastGameElementsHash

var m_RequestLock = false
var m_RequestQueue = []


# Called when the node enters the scene tree for the first time.
func _ready():
	
	RenderingServer.set_default_clear_color(Color.WEB_GRAY)
	
	pass


func AddToRequestQueue(url, httpMethod, content):
	
	m_RequestQueue.append([ url, httpMethod, content ])
	pass


# Rolls a dice
func _on_RollDiceOnServer(value):
	
	var url = m_ServerPath + "/roll?user='" + m_PlayerName + "'"
	AddToRequestQueue(url, HTTPClient.METHOD_POST, value)
	
	pass


# Instructs the server to print information about an selected object
func _on_PrintInformation(gameElementNode):
	
	var url = m_ServerPath + "/printinfo?user='" + m_PlayerName + "'"
	AddToRequestQueue(url, HTTPClient.METHOD_POST, gameElementNode.name)
		
	pass


# Reports to the server, that a specific asset should be removed
func _on_RemoveElementFromServer(gameElementTexNode):
	
	var url = m_ServerPath + "/removeasset?user='" + m_PlayerName + "'"
	AddToRequestQueue(url, HTTPClient.METHOD_POST, gameElementTexNode.name)
	
	pass


# Reports the new location of an asset to the server
func _on_UpdateLocationFromClient(gameElement):
		
	var url = m_ServerPath + "/updateassetlocation?user='" + m_PlayerName + "'"
	var requirement = gameElement.name + "\t" + str(gameElement.position.x) + "\t" + str(gameElement.position.y)

	AddToRequestQueue(url, HTTPClient.METHOD_POST, requirement)
	
	pass


# Reports the new size of an asset to the server
func _on_UpdateSizeFromClient(gameElement):
	
	var url = m_ServerPath + "/updateassetsize?user='" + m_PlayerName + "'"
	var requirement = gameElement.name + "\t" + str(gameElement.GetSizeX()) + "\t" + str(gameElement.GetSizeY())
	
	AddToRequestQueue(url, HTTPClient.METHOD_POST, requirement)
	
	pass


# Spawns a new asset, and reports it to the server.
func _on_SpawnNewAsset(assetName):
	
	var url = m_ServerPath + "/spawnasset?user='" + m_PlayerName + "'"
	var requirement = assetName + "\t" + str(get_viewport().size.x) + "\t" + str(get_viewport().size.y)
	
	AddToRequestQueue(url, HTTPClient.METHOD_POST, requirement)
	
	pass


# Writes text to the server chat
func _on_WriteChat(text):
	
	var url = m_ServerPath + "/addchat?user='" + m_PlayerName + "'"
	
	AddToRequestQueue(url, HTTPClient.METHOD_POST, text)
	
	pass


# Uploads assets to the server
func _on_UploadAssets(assets):
	
	for i in range(0, assets.size()):
		
		var fileNames = assets[i].split("/")
		var fileName = fileNames[fileNames.size() - 1].to_lower()
		var fileAccess = FileAccess.open(assets[i], FileAccess.READ)
		var fileContent = fileAccess.get_buffer(fileAccess.get_length())

		var url = m_ServerPath + "/postasset?name='" + fileName + "'"
		
		AddToRequestQueue(url, HTTPClient.METHOD_POST, fileContent)
	
	pass


# Show message function
func ShowInternalMessage(message, retToHandle):

	var messageScn = preload("res://MessageScn.tscn").instantiate()
	add_child(messageScn)
	messageScn.ShowMessage(message, retToHandle)
	
	pass


func _on_LeaveGame():
	
	$SyncTimer.stop()
	
	GameSession = null
	
	$MapScn.PerformCleanup()
	$GameHUD.hide()
	$MainMenu.show()
	
	pass


# Join server
func _on_JoinGame(playerName, serverPath):
	
	if (playerName.is_empty() || serverPath.is_empty()):
		
		ShowInternalMessage("Warning: Please enter player name and a valid server!", $MainMenu)
		
		return
	
	m_PlayerName = playerName
	m_ServerPath = serverPath
	
	var url = m_ServerPath + "/getsession"
	
	var errCode = $ServerConnector.request(url, [], HTTPClient.METHOD_GET, m_PlayerName );
	
	if (errCode != 0):
		
		ShowInternalMessage("Error: Server not found - please check connection string!", $MainMenu)
		return
		
	$MainMenu.hide()
	$GameHUD.show()
	
	# Connect to session
	var response = await $ServerConnector.request_completed
	var gameSession = JSON.new()
	gameSession.parse(response[3].get_string_from_utf8(), true)
	GameSession = JSON.parse_string(gameSession.get_parsed_text())
	
	m_LastAssetHash = "0"
	m_LastChatHash = "0"
	m_LastGameElementsHash = "0"
	
	$SyncTimer.start(-1)
	
	pass


# Main game sync function
func _do_Sync():
	
	if (m_RequestLock):
		return
	
	m_RequestLock = true
	
	if (m_RequestQueue.size() > 0):
		
		while (m_RequestQueue.size() > 0):
			
			var qreq = m_RequestQueue[0]
			
			if (typeof(qreq[2]) == TYPE_STRING || typeof(qreq[2]) == TYPE_STRING_NAME):
				var _qErrCode = $ServerConnector.request(qreq[0], [], qreq[1], qreq[2]);
			else:
				var _qErrCode = $ServerConnector.request_raw(qreq[0], [], qreq[1], qreq[2]);
			
			var _qResponse = await $ServerConnector.request_completed
			m_RequestQueue.remove_at(0)
	
	var url = m_ServerPath + "/getsession"
	
	var _errCode = $ServerConnector.request(url, [], HTTPClient.METHOD_GET, m_PlayerName );
	
	var response = await $ServerConnector.request_completed
	var gameSession = JSON.new()
	gameSession.parse(response[3].get_string_from_utf8(), true)
	GameSession = JSON.parse_string(gameSession.get_parsed_text())
	
	if (m_LastAssetHash != GameSession.AssetHash):
		
		for asset in GameSession.AssetList:
			
			var targetDir = "user://" + GameSession.SessionName
			var targetAsset = targetDir + "/" + asset
			
			DirAccess.make_dir_absolute(targetDir)
			
			if (FileAccess.file_exists(targetAsset)):
				
				var existingFile = FileAccess.open(targetAsset, FileAccess.READ)
				
				url = m_ServerPath + "/askassetsize?name='" + asset + "'"
			
				_errCode = $ServerConnector.request(url, [], HTTPClient.METHOD_GET)
				var assetSize = await $ServerConnector.request_completed
				
				var existingSize = existingFile.get_length()
				
				var buffer = StreamPeerBuffer.new()
				buffer.data_array = assetSize[3]
				var newSize = buffer.get_16()
				
				if (existingSize == newSize):
				
					continue
			
			url = m_ServerPath + "/getasset?name='" + asset + "'"
			
			_errCode = $ServerConnector.request(url, [], HTTPClient.METHOD_GET)
			var assetContent = await $ServerConnector.request_completed
			
			var fileAccess = FileAccess.open(targetAsset, FileAccess.WRITE)
			var fileContent = assetContent[3]
			
			fileAccess.store_buffer(fileContent)
		
		$GameHUD.UpdateAssetList(GameSession)
		
		m_LastAssetHash = GameSession.AssetHash
	
	if (m_LastChatHash != GameSession.ChatHash):
		
		$GameHUD.SetChatTextArray(GameSession.ChatWindowLog)
		m_LastChatHash = GameSession.ChatHash
	
	if (m_LastGameElementsHash != GameSession.GameElementsHash):
		
		var displayedObjects = $MapScn.GetDisplayableObjects()

		# Check if spawn is required
		for synced in GameSession.GameElements:
			var isNotDisplayed = true
			var positionMismatch = false
			var sizeMismatch = false
			
			for displayed in displayedObjects:
				
				if (displayed.name == synced.Identifier):
					isNotDisplayed = false
					
					if (synced.Location[0] != displayed.GetPosX() || synced.Location[1] != displayed.GetPosY()):
						positionMismatch = true
					
					if (synced.Size[0] != displayed.GetSizeX() || synced.Size[1] != displayed.GetSizeY()):
						sizeMismatch = true
					
					break
				
			if (isNotDisplayed):
				$MapScn.SpawnAsset(synced)
			
			if (positionMismatch):
				$MapScn.UpdateLocationFromServer(synced)
			
			if (sizeMismatch):
				$MapScn.UpdateSizeFromServer(synced)
		
		
		# Check if despawn required
		for displayed in displayedObjects:
			var isNotInSync = true
			var positionMismatch = false
			#var sizeMismatch = false
			
			for synced in GameSession.GameElements:
				
				if (displayed.name == synced.Identifier):
					isNotInSync = false
					
					if (synced.Location[0] != displayed.GetPosX() || synced.Location[1] != displayed.GetPosY()):
						positionMismatch = true
					
					#if (synced.Size[0] != displayed.GetSizeX() || synced.Size[1] != displayed.GetSizeY()):
						#sizeMismatch = true
					
					break
				
			if (isNotInSync):
				$MapScn.DespawnAsset(displayed)
			
			if (positionMismatch):
				$MapScn.UpdateLocation(displayed)
			
			#if (sizeMismatch):
				#$MapScn.UpdateSize(displayed)
		
		m_LastGameElementsHash = GameSession.GameElementsHash
		
		pass
	
	m_RequestLock = false
	
	pass


# Start server
func _on_ServerStart(serverName):

	if (serverName.is_empty()):
		
		ShowInternalMessage("Warning: Please enter a server!", $MainMenu)
		
		return

	# ToDo: Check, if path is valid when this program is compiled
	var applicationPath = OS.get_executable_path().split('/')
	var executingDir = ""
	
	for i in range(0, applicationPath.size() - 1):
		executingDir += applicationPath[i] + "/"
	
	var serverexec = executingDir + "server/serverexec.exe"
	
	if (FileAccess.file_exists(serverexec)):
		
		OS.create_process(serverexec, [ serverName ], true)
		
	else:
		
		ShowInternalMessage("Error: Failed to start server - executable not found: " + serverexec, $MainMenu)

	pass


# Show host menu
func _on_HostMenuClick():
	
	$MainMenu.hide()
	$HostMenu.show()
	
	pass


# Show join menu
func _on_JoinMenuClick():
	
	$MainMenu.hide()
	$JoinMenu.show()
	
	pass


# Show main menu
func _on_ShowMainMenuClick():
	
	$HostMenu.hide()
	$JoinMenu.hide()
	$MainMenu.show()
	
	pass



# Represents the game state synced from the server
var GameSession = {
	SessionName = "",
	BackgroundAsset = "",
	AssetHash = "",
	ChatHash = "",
	GameElementsHash = "",
	AssetList = [],
	PlayerList = [],
	ChatWindowLog = [],
	GameElements = [{ 
		Name = "",
		Owner = "",
		Identifier = "",
		Location = [],
		Size = []
	 }]
}
