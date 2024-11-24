# Variables
BUILD_DIR = build
CMAKE_BUILD_TYPE = Debug
TARGET = shaderpixel

# Default target
all:
	make configure 
	make building

# Configure the project (run CMake)
configure:
	cmake -B$(BUILD_DIR) -DCMAKE_BUILD_TYPE=$(CMAKE_BUILD_TYPE)

# Build the project
building:
	cmake --build $(BUILD_DIR)
	cp ./$(BUILD_DIR)/Debug/$(TARGET) .


# Clean the build directory
clean:
	rm -rf $(BUILD_DIR)

fclean : 
	make clean
	rm -f imgui.ini
	rm -f $(TARGET)

re: 
	make fclean 
	make all

.PHONY: all configure building clean fclean re