import tkinter as tk
from tkinter import messagebox
from utils import getSize


# Define all boxes
def formBox(window, **kwargs):
    right_label_keys = ['bg', 'highlightbackground', 'highlightthickness']  # Params for frame design
    right_geometry_keys = ['relx', 'rely', 'relwidth', 'relheight', 'anchor']  # Params for frame geometry
    right_label_values = {}  # Dict needed for mapping attributes used on frame design
    right_geometry_values = {}  # Dict needed for mapping attributes used on frame geometry
    # Validate attrbs
    for key, value in kwargs.items():
        if key in right_label_keys:
            right_label_values[key] = value
        elif key in right_geometry_keys:
            right_geometry_values[key] = value
    right_frame = tk.Frame(window, **right_label_values)  # Create frame with given attributes
    right_frame.place(**right_geometry_values)  # Place frame
    return right_frame


# Define all buttons
def button(right_frame, **kwargs):
    button_params = {}  # Dict needed for mapping attributes used on button design
    button_geometry_params = {}  # Dict needed for mapping attributes used on button geometry
    button_geometry_key = ['relx', 'rely', 'relwidth', 'relheight', 'anchor']  # Geometry params
    button_key = ['bg', 'text', 'command', 'font', 'fg', 'bd']  # Design params
    # Check params
    for key, value in kwargs.items():
        if key in button_geometry_key:
            button_geometry_params[key] = value
        elif key in button_key:
            button_params[key] = value
    btn = tk.Button(right_frame, **button_params)  # Create button with given params
    btn.place(**button_geometry_params)  # Place button


# Define all text
def rightText(frame, **kwargs):
    text_params = {}  # Dict needed for mapping used in texts design
    geometry_params = {}  # Dict needed for mapping used in texts geometry
    param_keys = ['text', 'bg', 'fg', 'font', 'anchor', 'wraplength', 'justify']  # Design attributes
    geometry_keys = ['relx', 'rely', 'relwidth', 'relheight', 'anchor_geometry']  # Geometry attributes
    # Validate attrbs
    for key, value in kwargs.items():
        if key in param_keys:
            text_params[key] = value
        elif key in geometry_keys:
            geometry_params['anchor' if key == 'anchor_geometry' else key] = value
    text_label = tk.Label(frame, **text_params)  # Create text with given attributes
    text_label.place(**geometry_params)  # Place text


# Filter placeholder
def filterLabel(filter_entry, placeholder_text):
    def onEntryClick(event):
        if filter_entry.get() == placeholder_text:
            filter_entry.delete(0, 'end')
            filter_entry.config(fg='black')

    def onFocusOut(event):
        if filter_entry.get() == '':
            filter_entry.insert(0, placeholder_text)
            filter_entry.config(fg='grey')

    filter_entry.insert(0, placeholder_text)
    filter_entry.config(fg='grey')
    filter_entry.bind('<FocusIn>', onEntryClick)
    filter_entry.bind('<FocusOut>', onFocusOut)


# Filter area
def dataFilter(frame):
    filter_entry = tk.Entry(frame, font='Arial, 11', bg='lightgray')  # Define filter area
    filter_entry.place(relx=1, rely=1, relheight=1, relwidth=1, anchor='se')  # Place filter area
    filterLabel(filter_entry, 'Filtrar por...')  # Set filter label
    return filter_entry


# Main Table, define scrolls bars
def canvasMainTable(parent):
    canvas = tk.Canvas(parent, bg='lightblue')
    scroll_y = tk.Scrollbar(parent, orient='vertical', command=canvas.yview, highlightbackground='black',
                            highlightthickness='0.4')  # Vertical scroll
    scroll_x = tk.Scrollbar(parent, orient='horizontal', command=canvas.xview)  # Horizontal scroll
    canvas.configure(yscrollcommand=scroll_y.set, xscrollcommand=scroll_x.set)  # Configure scroll bars on canvas
    canvas.place(relx=0, rely=0, relwidth=1, relheight=1)  # Place canvas
    scroll_y.place(relx=1, rely=0, relheight=1, anchor='ne')  # Place vertical scroll
    scroll_x.place(relx=0, rely=1, relwidth=0.986, anchor='sw')  # Place horizontal scroll
    interior = tk.Frame(canvas)  # Interior on canvas for tables
    canvas.create_window((0, 0), window=interior, anchor='nw')  # Set " frame for the tables"
    interior.bind("<Configure>", lambda e: canvas.configure(scrollregion=canvas.bbox("all")))  # Define scroll "size"


# Button status
def bttonStatus(window, title, message):
    if messagebox.askyesno(title, message):  # Btton confirmation to close the app
        window.destroy()
