#region Movement
// Calculate gravity and movement for next frame
// Calculate gravity
if (current_velocity_y < max_falling_speed)
{
	current_velocity_y += falling_rate; // Accelerate falling speed
}else
{
	current_velocity_y = max_falling_speed; // Cap falling speed
}

// Calculate Movement
if keyboard_check(ord("A"))
{
	current_velocity_x = -movement_speed;
}else if keyboard_check(ord("D"))
{
	current_velocity_x = movement_speed;
}else
{
	current_velocity_x = 0;
}

var new_velocity = new Vector2(x + current_velocity_x, y + current_velocity_y);

// Draw collider bounding box
var top_left_col = new Vector2(new_velocity.x - (sprite_width/2), new_velocity.y - (sprite_height/2));
var top_right_col = new Vector2(new_velocity.x + (sprite_width/2), new_velocity.y - (sprite_height/2));
var bottom_left_col = new Vector2(new_velocity.x - (sprite_width/2), new_velocity.y + (sprite_height/2));
var bottom_right_col = new Vector2(new_velocity.x + (sprite_width/2), new_velocity.y + (sprite_height/2));

// Check collisions for each scenario
var tiledata_top_left = tilemap_get_at_pixel(wall_tilemap, top_left_col.x, top_left_col.y);
var tiledata_top_right = tilemap_get_at_pixel(wall_tilemap, top_right_col.x, top_right_col.y);
var tiledata_bottom_left = tilemap_get_at_pixel(wall_tilemap, bottom_left_col.x, bottom_left_col.y);
var tiledata_bottom_right = tilemap_get_at_pixel(wall_tilemap, bottom_right_col.x, bottom_right_col.y);

// Apply values according to each scenario
// Gravity
if (tiledata_bottom_left > 0 || tiledata_bottom_right > 0) // bottom touching ground
{
	current_velocity_y = 0;
	can_jump = true;
}else
{
	can_jump = false;
}

// Ceiling
if (tiledata_top_left > 0 || tiledata_top_right > 0)
{
	current_velocity_y = 0;	
	can_jump = true;
}

if (tiledata_bottom_left > 0 || tiledata_bottom_right > 0
	|| tiledata_top_left > 0 || tiledata_top_right > 0)
{
	current_velocity_x = 0;
}

x += current_velocity_x;
y += current_velocity_y;

smooth_snap(new_velocity.x, new_velocity.y);

function smooth_snap(new_x, new_y)
{
	
	xCheck = 0;
	yCheck = 0;
	
	if (new_x - x > 0) xCheck = 1;
	if (new_x - x < 0) xCheck = -1;
	
	if (new_y - y > 0) yCheck = 1;
	if (new_y - y < 0) yCheck = -1;
	
	vert_wall_hit = false;
	hor_wall_hit = false;
	
	show_debug_message($"Check direction : {xCheck}, {yCheck}");
	if (!place_meeting(new_x, new_y, wall_tilemap)) return;
	
	show_debug_message($"Not hitting wall at : {new_x}, {new_y}");
	
	while(!hor_wall_hit)
	{
		if (place_meeting(x, y, wall_tilemap))
		{
			hor_wall_hit = true;
		}else
		{
			x += xCheck;
			vert_wall_hit = true;
		}
	}
	
	while (!vert_wall_hit)
	{
		if (place_meeting(x, y, wall_tilemap))
		{
			vert_wall_hit = true;
		}else
		{
			y += yCheck;
		}
	}
}
	
#endregion

#region Water Collision
if (place_meeting(x, y, obj_Water)){
	room_restart();
}
#endregion
// If player somehow leaves scene
if (x >= 2000 || y >= 2000 || x <= 0 || y <= 0)
{
	room_restart();
}
