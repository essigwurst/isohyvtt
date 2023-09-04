# (C)2023 Essigstudios Austria, All rights reserved.

extends Node2D

signal roll_dice(value)

# Set up button press events
func _ready():
	
	for button in $DicePanel.get_children():
		button.connect("pressed", _do_DiceClick.bind(button.text))
	

func _do_DiceClick(value):
	
	emit_signal("roll_dice", value)
	
	pass
