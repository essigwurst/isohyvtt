# (C)2023 Essigstudios Austria, All rights reserved.

extends Node2D

var parentScene


# Sets the message
func ShowMessage(message, parent):
	
	$MessageLabel.text = message
	parentScene = parent
	parentScene.hide()
	
	pass


# Hides this window
func _on_ConfirmClick():
	
	hide()
	parentScene.show()
	queue_free()
	
	pass
