#region Falling
//Falling
if (current_velocity_y < max_falling_speed)
{
	current_velocity_y += falling_rate; // Accelerate falling speed
}else
{
	current_velocity_y = max_falling_speed; // Cap falling speed
}

new_y = y + current_velocity_y;

check_ceiling_floor_collision();

function check_ceiling_floor_collision()
{
	// Check collision with tiles at the new position
	// We check multiple points along the bottom of the player sprite
	// Vertical collision detection
	
	var check_points = 3; // Number of points to check along the bottom

	for (var i = 0; i < check_points; i++)
	{
		var check_x = x - sprite_width/2 + (sprite_width * i / (check_points - 1));
		var check_bottom_y = new_y + sprite_height/2; // Bottom of sprite
		var check_top_y = new_y - sprite_height/2; // Top of sprite
    
		// Get the tile at this position
		var tile_data_bottom = tilemap_get_at_pixel(wall_tilemap, check_x, check_bottom_y);
		var tile_data_top = tilemap_get_at_pixel(wall_tilemap, check_x, check_top_y);
    
		// If there's a tile (tile_data > 0), we have a collision
		if (tile_data_bottom > 0)
		{
			floor_hit();
			break;
		}else if (tile_data_top > 0)
		{
			ceiling_hit();
			break;
		}else{
			can_jump = false;
			y = new_y;
		}
	}
}

function ceiling_hit()
{
	current_velocity_y = 0;
}
	
function floor_hit()
{
	// Find the exact Y position to place the player on top of the tile
    var tile_size = 32; // Adjust this to match your tile size
    var tile_y = floor((new_y + sprite_height/2) / tile_size) * tile_size;
    y = tile_y - sprite_height/2;
    current_velocity_y = 0;
	can_jump = true;
}

#endregion













if (x >= 2000 || y >= 2000)
{
	room_restart();
}