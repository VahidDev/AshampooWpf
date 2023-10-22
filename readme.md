# AshampooWpf

## Overview
AshampooWpf is a simple WPF application that provides a list of all drives and logical volumes on your computer. With a simple click, you can search for directories containing individual files larger than 10 MB on the selected drive or volume. The app utilizes parallel processing for faster performance, and you can pause and resume the search as needed.

As you run the search, discovered directories are immediately shown in the main window. For each directory, you'll see the total count of files and their combined size, not including sub-directory contents.

## Usage
1. Launch the application.
2. Click the "Search" button.
3. Pause or resume the search with the designated button as needed.
4. View discovered directories and their file statistics in real-time.

## Technical Details
*Design Pattern: MVVM
*No IOC container or factory pattern is used.
*Business and presentation layers are combined in a single project.