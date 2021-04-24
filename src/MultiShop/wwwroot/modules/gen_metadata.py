import os
import json

modulepaths = []

for content in os.listdir(os.getcwd()):
    if (os.path.isfile(content) and os.path.splitext(content)[1] == ".dll"):
        print("Adding \"{0}\" to list of modules.".format(content))
        modulepaths.append(content)

file = open("modules_content.json", "w")
json.dump(modulepaths, file, sort_keys=True, indent=4)
file.close()