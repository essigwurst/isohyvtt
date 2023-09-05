# (C)2023 Essigstudios Austria, All rights reserved.

extends Node


# Game element scale & drag and drop variables
var isDraggingElement = null
var lastMousePos = [ 1, 1 ]
var isDragging = false
var isScalingElement = null
var isScaling = false
var isRmbPressed = false

# Game element node
var GameElementNode = preload("res://GameElementNode.tscn")

# User interaction signals
signal update_location_from_client(gameElement)
signal update_size_from_client(gameElement)
signal remove_game_element(gameElement)
signal print_info(gameElement)


# Called every frame
func _process(_delta):
	
	if (isDraggingElement != null && isDragging):
		
		var dx = get_viewport().get_mouse_position().x - lastMousePos[0]
		var dy = get_viewport().get_mouse_position().y - lastMousePos[1]
		
		isDraggingElement.SetPosX(isDraggingElement.GetPosX() + dx)
		isDraggingElement.SetPosY(isDraggingElement.GetPosY() + dy)
		
		lastMousePos[0] = get_viewport().get_mouse_position().x
		lastMousePos[1] = get_viewport().get_mouse_position().y
	
	if (isScalingElement != null && isScaling):
		
		# See SetSizeX() function for details
		#var dx = get_viewport().get_mouse_position().x - lastMousePos[0]
		var dy = get_viewport().get_mouse_position().y - lastMousePos[1]
		
		#isScalingElement.SetSizeX(isScalingElement.GetSizeX() + dx)
		isScalingElement.SetSizeY(isScalingElement.GetSizeY() + dy)
		
		lastMousePos[0] = get_viewport().get_mouse_position().x
		lastMousePos[1] = get_viewport().get_mouse_position().y
	
	pass


# Handles scale & drag and drop options
func _on_GameElementMouseEvent(_viewport, inputEvent, _shapeIndex, gameElement):
	
	var isDragable = gameElement.is_dragable
	
	if (Input.is_mouse_button_pressed(MOUSE_BUTTON_LEFT) && isDragable):
		
		if (!isDragging):
			lastMousePos[0] = get_viewport().get_mouse_position().x
			lastMousePos[1] = get_viewport().get_mouse_position().y
		
		if (isDraggingElement != gameElement && isDragging):
			return
		
		isDraggingElement = gameElement
			
		isDragging = true;
	
	if (!Input.is_mouse_button_pressed(MOUSE_BUTTON_LEFT) && isDragging && isDragable):
		
		emit_signal("update_location_from_client", gameElement)
		
		lastMousePos[0] = 0
		lastMousePos[1] = 0
		
		isDraggingElement = null
		isDragging = false
	
	
	if (Input.is_mouse_button_pressed(MOUSE_BUTTON_MIDDLE) && isDragable):
		
		if (!isScaling):
			lastMousePos[0] = get_viewport().get_mouse_position().x
			lastMousePos[1] = get_viewport().get_mouse_position().y
		
		if (isScalingElement != gameElement && isScaling):
			return
		
		isScalingElement = gameElement
		isScaling = true
	
	if (!Input.is_mouse_button_pressed(MOUSE_BUTTON_MIDDLE) && isScaling && isDragable):
		
		emit_signal("update_size_from_client", gameElement)
		
		lastMousePos[0] = 0
		lastMousePos[1] = 0
		
		isScalingElement = null
		isScaling = false
	
	
	if (Input.is_mouse_button_pressed(MOUSE_BUTTON_RIGHT)):
		
		isRmbPressed = true
	
	if (!Input.is_mouse_button_pressed(MOUSE_BUTTON_RIGHT) && isRmbPressed):
		
		isRmbPressed = false

		for child in gameElement.get_children():
			
			if (child.name == "InvPopupButton"):
				
				child.position.x = inputEvent.position.x - gameElement.GetPosX()
				child.position.y = inputEvent.position.y - gameElement.GetPosY()
				child.show_popup()
				
				break
	
	pass


# Updates the size of a game element after a resizing was done by another person
func UpdateSizeFromServer(gameElementStruct):
	
	for child in get_children():
		
		if (child.name == gameElementStruct.Identifier):
			child.SetSizeX(gameElementStruct.Size[0])
			child.SetSizeY(gameElementStruct.Size[1])
			
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
	
	gameElement.SetSelfModulate(Color.FLORAL_WHITE)
	
	pass


# Remove hover effect
func _on_AssetMouseLeave(gameElement):
	
	gameElement.SetSelfModulate(Color.WHITE)
	
	pass


# Handles popup menu events
func _on_PopupMenuItemClick(id, gameElementNode):
	
	if (gameElementNode == null):
		return
	
	# Layer +
	if (id == 0):
		
		var nodeIndex = gameElementNode.get_index()
		var nodeCount = get_child_count()
		var targetIndex = nodeIndex + 1
		
		if (targetIndex >= nodeCount):
			targetIndex = nodeIndex
			gameElementNode.z_index = gameElementNode.z_index - 1
		
		move_child(gameElementNode, targetIndex)
		gameElementNode.z_index = gameElementNode.z_index + 1
		
	# Layer -
	elif (id == 1):
		
		var nodeIndex = gameElementNode.get_index()
		var targetIndex = nodeIndex - 1
		
		if (targetIndex < 0):
			targetIndex = nodeIndex
			gameElementNode.z_index = gameElementNode.z_index + 1
			
		move_child(gameElementNode, targetIndex)
	
		gameElementNode.z_index = gameElementNode.z_index - 1
	
	# Prints info to the chat box
	elif (id == 2):
		emit_signal("print_info", gameElementNode)
		
	# Delete
	elif (id == 3):
		emit_signal("remove_game_element", gameElementNode)
	
	pass


# Spawns a new element on the screen
func SpawnAsset(gameElementStruct):
	
	var targetDir = "user://" + get_parent().GameSession.SessionName
	var targetAsset = targetDir + "/" + gameElementStruct.Name
	
	var gameElement = GameElementNode.instantiate()
	
	gameElement.Init(gameElementStruct, targetAsset)
	
	if (get_parent().m_PlayerName == gameElementStruct.Owner):
		gameElement.is_dragable = true
		gameElement.connect("mouse_entered", _on_AssetMouseOver.bind(gameElement))
		gameElement.connect("mouse_exited", _on_AssetMouseLeave.bind(gameElement))
	else:
		gameElement.is_dragable = false
	
	gameElement.connect("input_event", _on_GameElementMouseEvent.bind(gameElement))
	
	# Add popup menu - probably there is a better use case?
	var invisibleButton = MenuButton.new()
	invisibleButton.name = "InvPopupButton"
	invisibleButton.get_popup().add_item("Layer +")
	invisibleButton.get_popup().add_item("Layer -")
	invisibleButton.get_popup().add_item("Information")
	
	# Add remove function only, if this is the owner of the node
	if (get_parent().m_PlayerName == gameElementStruct.Owner):
		invisibleButton.get_popup().add_item("Remove")
	
	invisibleButton.get_popup().connect("id_pressed", _on_PopupMenuItemClick.bind(gameElement))
	invisibleButton.size.x = 1
	invisibleButton.size.y = 1
	invisibleButton.visible = false
	
	gameElement.add_child(invisibleButton, true)
	self.add_child(gameElement, true)

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
