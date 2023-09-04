# (C)2023 Essigstudios Austria, All rights reserved.

extends Node2D

signal host_menu
signal join_menu

# Host game
func _on_HostButton_pressed():
	
	emit_signal("host_menu")
	
	pass


# Join game
func _on_JoinButton_pressed():
	
	emit_signal("join_menu")
	
	pass


# End game
func _on_ExitButton_pressed():
	
	get_tree().quit()
	
	pass
