# from tkinter import *
# from tkinter import ttk
from Core import draw_view
# import tkinter.messagebox

# TitleName = "APP Performance"
# WindowSize = "960x640+700+400"
#
# class MainWindow:
#     def __init__(self):
#         pass
#
#     def start(self):
#         self.window = Tk()
#         self.window.title(TitleName)
#         self.window.geometry(WindowSize)
#         self.window.resizable(width=False,height=False)
#         self.window.config(bg="black")
#         self.draw_select_devices(self.window)
#         self.draw_select_view(self.window)
#         self.window.mainloop()
#         pass
#
#     def draw_select_view(self,win):
#         pass
#         # CheckVar1 = IntVar()
#         # CheckVar2 = IntVar()
#         # CheckVar3 = IntVar()
#         # CheckVar4 = IntVar()
#         # # 设置三个复选框控件，使用variable参数来接收变量
#         # check1 = Checkbutton(win, text="卡罗拉", font=('微软雅黑', 15, 'bold'), variable=CheckVar1, onvalue=1,
#         #                      offvalue=0)
#         # check2 = Checkbutton(win, text="凯美瑞", font=('微软雅黑', 15, 'bold'), variable=CheckVar2, onvalue=1,
#         #                      offvalue=0)
#         # check3 = Checkbutton(win, text="亚洲龙", font=('微软雅黑', 15, 'bold'), variable=CheckVar3, onvalue=1,
#         #                      offvalue=0)
#         # check4 = Checkbutton(win, text="雷凌", font=('微软雅黑', 15, 'bold'), variable=CheckVar4, onvalue=1, offvalue=0)
#         # check1.pack(side=LEFT)
#         # check2.pack(side=LEFT)
#         # check3.pack(side=LEFT)
#         # check4.pack(side=LEFT)
#
#
#
#     def draw_select_devices(self,win):
#         devices = util.scan_devices()
#         if len(devices) <= 0:
#             tkinter.messagebox.showwarning("警示", "未连接手机设备，请检查！")
#             return
#         deviceID = devices[0]['id']
#         print(deviceID)
#         self.ddl = ttk.Combobox(win)
#         self.ddl['value'] = ('下拉选项1', '下拉选项2', '下拉选项3', '下拉选项4')
#         # 设置默认值，即默认下拉框中的内容,索引从0开始
#         self.ddb_default_L = Label(win, text='下拉框默认值：')
#         self.ddb_default = ttk.Combobox(win)
#         self.ddb_default['value'] = ('下拉选项1', '下拉选项2', '下拉选项3', '下拉选项4')
#         self.ddb_default.current(2)
#



# Press the green button in the gutter to run the script.
if __name__ == '__main__':
     # window = MainWindow()
     # window.start()
     draw_view.draw_file("Test.txt","Test.png")


# See PyCharm help at https://www.jetbrains.com/help/pycharm/
