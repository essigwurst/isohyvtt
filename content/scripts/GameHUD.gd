# (C)2023 Essigstudios Austria, All rights reserved.

extends Node2D

signal leave_game
signal upload_assets(assets)
signal write_chat_message(text)
signal spawn_asset(assetName)
signal roll_dice_tunnel(value)

var m_SelectedFiles


# Called, when the GameHUD is first shown
func _ready():
	
	var allowed = ["*jpg", "*png", "*bmp"]
	$LocalAssetSelectionDialog.set_filters(allowed)

	pass


# Opens or closes dice roll menu
func _on_RollDiceClick():
	
	if ($DiceMenu.visible):
		$DiceMenu.hide()
	else:
		$DiceMenu.show()
	
	pass


# Rolls a dice - tunneled event from DiceMenu
func _on_RollDiceTunnel(value):
	
	emit_signal("roll_dice_tunnel", value)
	
	pass


# Emits a new spawn asset action
func _on_SpawnAssetClick(_mouseEvent, assetTextureNode):
	
	if (Input.is_mouse_button_pressed(MOUSE_BUTTON_LEFT)):
		
		emit_signal("spawn_asset", assetTextureNode)
		
		$SpawnBlockTimer.one_shot = true
		$SpawnBlockTimer.start()
		
	pass


# Enable hover
func _on_AssetIconMouseOver(assetTextureNode):
	
	assetTextureNode.self_modulate = Color.ORANGE_RED
	
	pass


# Disable hover
func _on_AssetIconMouseLeave(assetTextureNode):
	
	assetTextureNode.self_modulate = Color.WHITE
	
	pass


# Updates the asset list
func UpdateAssetList(gameSession):
	
	for childNode in $AssetScrollContainer/AssetListContainer.get_children():
		
		$AssetScrollContainer/AssetListContainer.remove_child(childNode)
		childNode.queue_free()
		
	
	var targetDir = "user://" + gameSession.SessionName
	
	for assetName in gameSession.AssetList:
		
		var targetAsset = targetDir + "/" + assetName
		var image = Image.new()
		var texNode = TextureRect.new()
		
		image.load(targetAsset)
		
		texNode.texture = ImageTexture.create_from_image(image)
		texNode.stretch_mode = TextureRect.STRETCH_KEEP_ASPECT
		texNode.expand_mode = TextureRect.EXPAND_FIT_WIDTH_PROPORTIONAL
		texNode.size.x = 32
		texNode.size.y = 32
		texNode.custom_minimum_size.x = 32
		texNode.custom_minimum_size.y = 32
		
		texNode.connect("mouse_entered", _on_AssetIconMouseOver.bind(texNode))
		texNode.connect("mouse_exited", _on_AssetIconMouseLeave.bind(texNode))
		texNode.connect("gui_input", _on_SpawnAssetClick.bind(assetName))
		
		$AssetScrollContainer/AssetListContainer.add_child(texNode)
	
	pass


# Writes a chat text back to the server
func _on_ChatTextInput(_event):
	
	if Input.is_key_pressed(KEY_ENTER):
		
		emit_signal("write_chat_message", $EnterTextBox.text)
		
		$ResetTextTimer.one_shot = true
		$ResetTextTimer.start()
	
	pass


# Clears the text box, after the text was sent
func _on_ChatTextTimerFire():

	$EnterTextBox.clear()
	pass


# Appends a text array from source
func SetChatTextArray(text):
	
	$LogTextBox.text = ""
	
	for line in text:
		
		$LogTextBox.text += line + "\n"

	$LogTextBox.set_caret_line($LogTextBox.get_line_count())
	
	pass


# Update internal selected files variable
func _on_FilesSelected(files):
	
	m_SelectedFiles = files
	
	pass


# Starts the file upload
func _on_OnUploadAssetsSelConfirmed():
	
	emit_signal("upload_assets", m_SelectedFiles)
	
	pass


# Shows upload menu (will auto- hide on confirm or cancel)
func _on_UploadAssetClick():
	
	$LocalAssetSelectionDialog.show()
	
	pass


# Emits a signal to leave the game and return back to the main menu
func _on_LeaveGameClick():
	
	emit_signal("leave_game")
	
	pass
