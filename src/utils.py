import tkinter as tk


# Get app size
def appSize(window):
    screen_width, screen_height = getSize(window)
    window_width = int(screen_width * 0.8)
    window_height = int(screen_height * 0.8)
    pos_x = int((screen_width - window_width) // 2)
    pos_y = int((screen_height - window_height) // 2)
    return f'{window_width}x{window_height}+{pos_x}+{pos_y}'


def getSize(window):
    window.update()
    width = window.winfo_screenwidth()
    height = window.winfo_screenheight()
    return width, height



