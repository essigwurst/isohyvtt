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

extends Area2D

# Public properties
@export var is_dragable: bool

# Private properties
var aspect_ratio_x = 1.0
var aspect_ratio_y = 1.0


# Assigns the values to the existing structures
func Init(gameElementStruct, targetAsset):

	var image = Image.new()	
	image.load(targetAsset)
	
	name = gameElementStruct.Identifier
	z_index = gameElementStruct.Layer
	position.x = gameElementStruct.Location[0]
	position.y = gameElementStruct.Location[1]
	
	aspect_ratio_x = gameElementStruct.Location[0] / gameElementStruct.Location[1]
	aspect_ratio_y = gameElementStruct.Location[1] / gameElementStruct.Location[0]
	
	$GameElementTexture.texture = ImageTexture.create_from_image(image)
	$GameElementTexture.stretch_mode = TextureRect.STRETCH_KEEP_ASPECT
	$GameElementTexture.expand_mode = TextureRect.EXPAND_FIT_WIDTH_PROPORTIONAL
	$GameElementTexture.size.x = gameElementStruct.Size[0]
	$GameElementTexture.size.y = gameElementStruct.Size[1]
	$GameElementTexture.texture_filter = CanvasItem.TEXTURE_FILTER_NEAREST
	
	var rectShape = RectangleShape2D.new()
	rectShape.size.x = gameElementStruct.Size[0]
	rectShape.size.y = gameElementStruct.Size[1]
	
	$GameElementCollisionShape.shape = rectShape
	$GameElementCollisionShape.position.x = gameElementStruct.Size[0] / 2
	$GameElementCollisionShape.position.y = gameElementStruct.Size[1] / 2
	
	pass


# Overlay color for mouse hovering
func SetSelfModulate(color):
	
	$GameElementTexture.self_modulate = color
	
	pass


# Sets the X position of this element
func SetPosX(value):
	
	position.x = value
	
	pass


# Sets the Y position of this element
func SetPosY(value):
	
	position.y = value
	
	pass


# Updates the collision shape from the texture
func _do_updateCollisionShapeFromTexture():
	
	$GameElementCollisionShape.position.x = $GameElementTexture.size.x / 2
	$GameElementCollisionShape.position.y = $GameElementTexture.size.y / 2
	$GameElementCollisionShape.shape.size.x = $GameElementTexture.size.x
	$GameElementCollisionShape.shape.size.y = $GameElementTexture.size.y
	
	pass


# Sets the new texture size X, and updates the collision shape
func SetSizeX(value):
	
	# Remark: It seems like scaling *does* happen only in y direction so this function is not in use anymore
	
	if (value < 2):
		return
	
	$GameElementTexture.size.x = value
	#$GameElementTexture.size.y = value * aspect_ratio_y
	_do_updateCollisionShapeFromTexture()
	
	#print("set x (" + str(value) + ") - [" + str($GameElementTexture.size.x) + " / " + str($GameElementTexture.size.y) + "]")
	
	pass


# Sets the new texture size Y, and updates the collision shape
func SetSizeY(value):
	
	if (value < 2):
		return
	
	$GameElementTexture.size.y = value
	$GameElementTexture.size.x = value * aspect_ratio_y
	_do_updateCollisionShapeFromTexture()
	
	# print("set y (" + str(value) + ") - [" + str($GameElementTexture.size.x) + " / " + str($GameElementTexture.size.y) + "]")
	
	pass


# Returns the X position of this element
func GetPosX():
	
	var area2dPosX = position.x
	
	return (area2dPosX)
	

# Returns the Y position of this element	
func GetPosY():
	
	var area2dPosY = position.y
	
	return (area2dPosY)
	

# Returns the width of the texture
func GetSizeX():
	
	var textureSizeX = int($GameElementTexture.size.x)
	
	return (textureSizeX)


# Returns the height of the texture
func GetSizeY():
	
	var textureSizeY = int($GameElementTexture.size.y)
	
	return (textureSizeY)


# Returns the z_index aka layer of this object
func GetLayer():
	
	return (z_index)
