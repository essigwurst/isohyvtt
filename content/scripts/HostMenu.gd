# (C)2023 Essigstudios Austria, All rights reserved.

extends Node2D

signal main_menu
signal start_server

# Host game
func _on_ServerStartButton_pressed():

	emit_signal("main_menu")
	emit_signal("start_server", $ServerNameTextBox.text)
	
	pass


# Join game
func _on_BackButton_pressed():
	
	emit_signal("main_menu")
	
	pass
