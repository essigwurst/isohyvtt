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

extends Node2D

signal roll_dice(value)

# Set up button press events
func _ready():
	
	for button in $DicePanel.get_children():
		button.connect("pressed", _do_DiceClick.bind(button.text))
	

func _do_DiceClick(value):
	
	emit_signal("roll_dice", value)
	
	pass
