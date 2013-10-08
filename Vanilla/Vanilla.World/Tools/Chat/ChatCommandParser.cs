﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Vanilla.World.Tools.Chat
{
    public class ChatCommandParser
    {
        private static readonly List<ChatCommandNode> ChatCommandNodes = new List<ChatCommandNode>();

        //TODO Cleanup parser and allow ignoring case.
        public static void Boot()
        {
            foreach (var type in GetTypesWithChatCommandNode(Assembly.GetExecutingAssembly()))
            {
                ChatCommandNode node = GetNode(type);
                node.Method = type.GetMethod("Default");
                AddNode(node);

                node.CommandAttributes = new List<ChatCommandAttribute>();

                foreach (var method in GetMethodsWithChatCommandAttribute(type))
                {
                    ChatCommandAttribute chatCommandAttribute = GetAttribute(method);
                    chatCommandAttribute.Method = method;
                    node.CommandAttributes.Add(chatCommandAttribute);
                }
            }
        }

        public static void AddNode(ChatCommandNode node)
        {
            ChatCommandNodes.Add(node);
        }

        public static void RemoveNode(ChatCommandNode node)
        {
            ChatCommandNodes.Remove(node);
        }

        public static void RemoveNode(String nodeName)
        {
            ChatCommandNodes.Remove(ChatCommandNodes.FirstOrDefault(n => n.Name == nodeName));
        }

        static IEnumerable<MethodInfo> GetMethodsWithChatCommandAttribute(Type type)
        {
            foreach (MethodInfo method in type.GetMethods())
            {
                if (method.GetCustomAttributes(typeof(ChatCommandAttribute), true).Length > 0)
                {
                    yield return method;
                }
            }
        }

        static IEnumerable<Type> GetTypesWithChatCommandNode(Assembly assembly)
        {
            foreach (Type type in assembly.GetTypes())
            {
                if (type.GetCustomAttributes(typeof(ChatCommandNode), true).Length > 0)
                {
                    yield return type;
                }
            }
        }

        private static ChatCommandNode GetNode(Type type)
        {
            Object[] attributes = type.GetCustomAttributes(typeof(ChatCommandNode), false);
            return (attributes.Length > 0) ? attributes.First() as ChatCommandNode : null;
        }

        private static ChatCommandAttribute GetAttribute(MethodInfo method)
        {
            Object[] attributes = method.GetCustomAttributes(typeof (ChatCommandAttribute), false);
            return (attributes.Length > 0) ? attributes.First() as ChatCommandAttribute : null;
        }

        public static Boolean ExecuteCommand(WorldSession sender, String message)
        {
            //Remove the chat command key
            message = message.Remove(0, INI.GetValue(ConfigSections.WORLD, ConfigValues.COMMAND_KEY).Length);
            List<String> args = message.ToLower().Split(' ').ToList();

            ChatCommandNode commandNode = ChatCommandNodes.FirstOrDefault(node => node.Name == args[0]);

            if (commandNode != null)
            {
                //remove the command node.
                args.RemoveAt(0);

                ChatCommandAttribute commandAttribute = args.Count > 0 ? commandNode.CommandAttributes.FirstOrDefault(attribute => attribute.Name == args[0]) : null;

                if (commandAttribute != null)
                {
                    //remove the attribute
                    args.RemoveAt(0);

                    object[] commandArguments = { sender, args.ToArray() };

                    //Call method with null instance (all command methods are static)
                    try
                    {
                        commandAttribute.Method.Invoke(null, commandArguments);
                        Log.Print(LogType.Debug, "Player " + sender.Character.Name + " used command " + commandNode.Name + " " + commandAttribute.Name);
                        return true;
                    }
                    catch (Exception e)
                    {
                        Log.Print(LogType.Error, "Command Errored");
                        Log.Print(LogType.Error, e.StackTrace);
                        sender.sendMessage("** " + commandNode.Name + " commands **");
                        sendCommandMessage(sender, commandAttribute);
                        return false;
                    }
                }
                if (commandNode.Method != null)
                {
                    object[] commandArguments = { sender, args.ToArray() };

                    try
                    {
                        commandNode.Method.Invoke(null, commandArguments);
                        Log.Print(LogType.Debug, "Player " + sender.Character.Name + " used command " + commandNode.Name + " Default");
                        return true;
                    }
                    catch (Exception)
                    {
                        Log.Print(LogType.Error, "Error using command: " + commandNode.Name);
                        Log.Print(LogType.Error, "Make sure the method passed in AddChatCommand is static!");
                        return false;
                    }
                }
                sender.sendMessage("** " + commandNode.Name + " commands **");
                commandNode.CommandAttributes.ForEach(a => sendCommandMessage(sender, a));
                return false;
            }
            sender.sendMessage("** commands **");
            ChatCommandNodes.ForEach(n => sendCommandMessage(sender, n));
            return false;
        }

        public static void sendCommandMessage(WorldSession session, ChatCommandBase cmd)
        {
            session.sendMessage(cmd.Name + " - " + cmd.Description);
        }
    }
}