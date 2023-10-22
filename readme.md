# AshampooWpf

# Description
Windows app that lists all the drives and logical volumes on the user's system. Upon clicking a button, the app searches
through all the files on the selected drive/volume and finds all directories that directly contain individual files larger than 10 MB. Processing
is be done in multiple threads in parallel to increase performance. It is possible to pause and resume the search by pressing a
button.
Any discovered directory is immediately shown in a separate result list in the main window while the search is still running. The app
also displays two numbers for each found directory: the count of all files (of any size) in the directory, as well as the combined size of
these files. The contents of sub-directories are not be included in these numbers.

# Notes
Business and Presentation layers are kept in one project
