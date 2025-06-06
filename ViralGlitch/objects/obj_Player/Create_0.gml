
// Gravity Settings
falling_rate = 1;
max_falling_speed = 50;

// Movement Settings
jumping_velocity = -15;

// Runtime Variables
current_velocity_x = 0;
new_x = 0;
current_velocity_y = 0;
new_y = 0;

can_jump = false;

// Collisions
wall_layer = layer_get_id("PowerStation_Walls");
wall_tilemap = layer_tilemap_get_id(wall_layer);