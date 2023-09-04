# (C)2023 Essigstudios Austria, All rights reserved.

extends Node2D

signal main_menu
signal join_game

# Join game
func _on_JoinButton_pressed():
	
	emit_signal("main_menu")
	emit_signal("join_game", $PlayerNameTextBox.text, $ServerPathTextBox.text)
	
	pass


# Join game
func _on_BackButton_pressed():
	
	emit_signal("main_menu")
	
	pass
