# (C)2023 Essigstudios Austria, All rights reserved.

extends Node


# Game element scale & drag and drop variables
var isDraggingElement = null
var lastMousePos = [ 1, 1 ]
var isDragging = false
var isScalingElement = null
var isScaling = false
var isRmbPressed = false

signal UpdateLocationFromClient(gameElement)
signal UpdateSizeFromClient(gameElement)
signal RemoveGameElement(gameElement)
signal PrintInfo(gameElement)

# Called every frame
func _process(_delta):
	
	if (isDraggingElement != null && isDragging):
		
		var dx = get_viewport().get_mouse_position().x - lastMousePos[0]
		var dy = get_viewport().get_mouse_position().y - lastMousePos[1]
		
		isDraggingElement.position.x += dx
		isDraggingElement.position.y += dy
		
		lastMousePos[0] = get_viewport().get_mouse_position().x
		lastMousePos[1] = get_viewport().get_mouse_position().y
	
	if (isScalingElement != null && isScaling):
		
		var dx = get_viewport().get_mouse_position().x - lastMousePos[0]
		var dy = get_viewport().get_mouse_position().y - lastMousePos[1]
		
		isScalingElement.size.x += dx
		isScalingElement.size.y += dy
		
		lastMousePos[0] = get_viewport().get_mouse_position().x
		lastMousePos[1] = get_viewport().get_mouse_position().y
	
	pass


# Handles scale & drag and drop options
func _on_GameElementMouseEvent(inputEvent, gameElement):
	
	var isDragable = gameElement.editor_description == "isDragable"
	
	if (Input.is_mouse_button_pressed(MOUSE_BUTTON_LEFT) && isDragable):
		
		if (!isDragging):
			lastMousePos[0] = get_viewport().get_mouse_position().x
			lastMousePos[1] = get_viewport().get_mouse_position().y
		
		isDraggingElement = gameElement
		isDragging = true;
	
	elif (!Input.is_mouse_button_pressed(MOUSE_BUTTON_LEFT) && isDragging && isDragable):
		
		emit_signal("UpdateLocationFromClient", gameElement)
		
		lastMousePos[0] = 0
		lastMousePos[1] = 0
		
		isDraggingElement = null
		isDragging = false
	
	
	if (Input.is_mouse_button_pressed(MOUSE_BUTTON_MIDDLE) && isDragable):
		
		if (!isScaling):
			lastMousePos[0] = get_viewport().get_mouse_position().x
			lastMousePos[1] = get_viewport().get_mouse_position().y
		
		isScalingElement = gameElement
		isScaling = true
	
	elif (!Input.is_mouse_button_pressed(MOUSE_BUTTON_MIDDLE) && isScaling && isDragable):
		
		emit_signal("UpdateSizeFromClient", gameElement)
		
		lastMousePos[0] = 0
		lastMousePos[1] = 0
		
		isScalingElement = null
		isScaling = false
	
	
	if (Input.is_mouse_button_pressed(MOUSE_BUTTON_RIGHT)):
		
		isRmbPressed = true
	
	if (!Input.is_mouse_button_pressed(MOUSE_BUTTON_RIGHT) && isRmbPressed):
		
		isRmbPressed = false
		
		# We have only one child node here - and it's the invisible button /w popup menu
		gameElement.get_child(0).position.x = inputEvent.position.x
		gameElement.get_child(0).position.y = inputEvent.position.y
		gameElement.get_child(0).show_popup()
	
	pass


# Updates the size of a game element after a resizing was done by another person
func UpdateSizeFromServer(gameElement):
	
	for child in get_children():
		
		if (gameElement is TextureRect):
			if (child.name == gameElement.name):
				child.size.x = gameElement.size.x
				child.size.y = gameElement.size.y
				break
		else:
			if (child.name == gameElement.Identifier):
				child.size.x = gameElement.Size[0]
				child.size.y = gameElement.Size[1]
				break
	pass


# Updates the location of a game element done by another person
func UpdateLocationFromServer(gameElement):
	
	for child in get_children():
		
		if (gameElement is TextureRect):
			if (child.name == gameElement.name):
				child.position.x = gameElement.position.x
				child.position.y = gameElement.position.y
				
				break
		else:
			if (child.name == gameElement.Identifier):
				child.position.x = gameElement.Location[0]
				child.position.y = gameElement.Location[1]
				
				break
	
	pass


# Add hover effect
func _on_AssetMouseOver(gameElement):
	
	gameElement.self_modulate = Color.FLORAL_WHITE
	
	pass


# Remove hover effect
func _on_AssetMouseLeave(gameElement):
	
	gameElement.self_modulate = Color.WHITE
	
	pass


# Handles popup menu events
func _on_PopupMenuItemClick(id, texNode):
	
	if (texNode == null):
		return
	
	# Layer +
	if (id == 0):
		texNode.z_index = texNode.z_index + 1
		
	# Layer -
	elif (id == 1):
		texNode.z_index = texNode.z_index - 1
	
	# Prints info to the chat box
	elif (id == 3):
		emit_signal("PrintInfo", texNode)
		
	# Delete
	elif (id == 3):
		emit_signal("RemoveGameElement", texNode)
	
	pass


# Spawns a new element on the screen
func SpawnAsset(gameElement):
	
	var targetDir = "user://" + get_parent().GameSession.SessionName
	var targetAsset = targetDir + "/" + gameElement.Name
	var image = Image.new()
	var texNode = TextureRect.new()
	
	image.load(targetAsset)
	
	texNode.name = gameElement.Identifier
	texNode.texture = ImageTexture.create_from_image(image)
	texNode.stretch_mode = TextureRect.STRETCH_KEEP_ASPECT
	texNode.expand_mode = TextureRect.EXPAND_FIT_WIDTH_PROPORTIONAL
	texNode.size.x = gameElement.Size[0]
	texNode.size.y = gameElement.Size[1]
	texNode.position.x = gameElement.Location[0]
	texNode.position.y = gameElement.Location[1]
	texNode.texture_filter = CanvasItem.TEXTURE_FILTER_NEAREST
	
	if (get_parent().m_PlayerName == gameElement.Owner):
		texNode.editor_description = "isDragable"
		texNode.connect("mouse_entered", _on_AssetMouseOver.bind(texNode))
		texNode.connect("mouse_exited", _on_AssetMouseLeave.bind(texNode))
	else:
		texNode.editor_description = "notDragable"
	
	texNode.connect("gui_input", _on_GameElementMouseEvent.bind(texNode))
	
	# Add popup menu - probably there is a better use case?
	var invisibleButton = MenuButton.new()
	invisibleButton.get_popup().add_item("Layer +")
	invisibleButton.get_popup().add_item("Layer -")
	invisibleButton.get_popup().add_item("Info")
	
	# Add remove function only, if this is the owner of the node
	if (get_parent().m_PlayerName == gameElement.Owner):
		invisibleButton.get_popup().add_item("Remove")
	
	invisibleButton.get_popup().connect("id_pressed", _on_PopupMenuItemClick.bind(texNode))
	invisibleButton.size.x = 1
	invisibleButton.size.y = 1
	invisibleButton.visible = false
	
	texNode.add_child(invisibleButton, true)
	self.add_child(texNode, true)

	pass


# Removes an element from the screen
func DespawnAsset(gameElement):
	
	for child in get_children():
		
		if (child.name == gameElement.name):
			
			child.queue_free()
			break
	pass


# Returns a list of currently displayed objects
func GetDisplayableObjects():
	
	var displayableObjects = self.get_children()
	
	return (displayableObjects)


# Clears the map
func PerformCleanup():

	for child in get_children():
		child.queue_free()
	
	pass
