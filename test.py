#!/usr/bin/env python3
import requests
import json
import time

# Base URL for the API
BASE_URL = "http://localhost:8001/api"

# Common headers for all requests
HEADERS = {
    'accept': 'text/plain',
    'Content-Type': 'application/json-patch+json',
    'RequestVerificationToken': 'CfDJ8LlJtsBNTnhJo6KO-VnW8ZIJ1ejj-TI4pRx4zG5mOrcR7WA_RUf-HDmVLiiv9_oqermBbimXZ8eV0mO8TvKePGA4D5jMYkEGSvFAM_UNefAZTHvjqR-BUNJSUavv9T4kaXz9DCH2au7GhZR0y15FRl0',
    'X-Requested-With': 'XMLHttpRequest'
}

def create_agent(agent_type, agent_name):
    """
    Create an agent of the specified type with the given name.
    
    Args:
        agent_type (str): Type of agent to create
        agent_name (str): Name for the agent
        
    Returns:
        dict: Agent details including ID if successful, None otherwise
    """
    url = f"{BASE_URL}/agent"
    payload = {
        "agentType": agent_type,
        "name": agent_name
    }
    
    print(f"Creating {agent_type} named {agent_name}...")
    
    try:
        response = requests.post(url, headers=HEADERS, json=payload)
        response.raise_for_status()
        
        result = response.json()
        if result.get("code") == "20000":
            agent_id = result["data"]["id"]
            print(f"✅ Successfully created {agent_type} with ID: {agent_id}")
            return result["data"]
        else:
            print(f"❌ Failed to create agent: {result.get('message')}")
            return None
    
    except requests.exceptions.RequestException as e:
        print(f"❌ Error creating agent: {str(e)}")
        return None

def register_subagent(parent_agent_id, subagent_id):
    """
    Register an agent as a subagent of another agent.
    
    Args:
        parent_agent_id (str): ID of the parent agent
        subagent_id (str): ID of the agent to register as subagent
        
    Returns:
        bool: True if successful, False otherwise
    """
    url = f"{BASE_URL}/agent/{parent_agent_id}/add-subagent"
    payload = {
        "subAgents": [subagent_id]
    }
    
    print(f"Registering agent {subagent_id} as subagent of {parent_agent_id}...")
    
    try:
        response = requests.post(url, headers=HEADERS, json=payload)
        response.raise_for_status()
        
        result = response.json()
        if result.get("code") == "20000":
            print(f"✅ Successfully registered subagent")
            return True
        else:
            print(f"❌ Failed to register subagent: {result.get('message')}")
            return False
    
    except requests.exceptions.RequestException as e:
        print(f"❌ Error registering subagent: {str(e)}")
        return False

def publish_message(agent_id, event_type, message):
    """
    Publish a message to an agent.
    
    Args:
        agent_id (str): ID of the agent to send message to
        event_type (str): Type of the event
        message (str): Message content
        
    Returns:
        bool: True if successful, False otherwise
    """
    url = f"{BASE_URL}/agent/publishEvent"
    payload = {
        "agentId": agent_id,
        "eventType": event_type,
        "eventProperties": {
            "message": message
        }
    }
    
    print(f"Publishing message to agent {agent_id}...")
    
    try:
        response = requests.post(url, headers=HEADERS, json=payload)
        response.raise_for_status()
        
        if response.status_code == 200:
            print(f"✅ Successfully published message")
            return True
        else:
            print(f"❌ Failed to publish message: {response.text}")
            return False
    
    except requests.exceptions.RequestException as e:
        print(f"❌ Error publishing message: {str(e)}")
        return False

def run_workflow():
    """
    Run the complete workflow to create agents, register subagents, and publish a message.
    """
    print("Starting agent setup workflow...\n")
    
    # Step 1: Create a creator agent
    creator_agent = create_agent("creatorgagent", "agent-100")
    if not creator_agent:
        print("❌ Workflow failed at creator agent creation step")
        return
    
    creator_id = creator_agent["id"]
    
    # Step 2: Create a code agent
    code_agent = create_agent("codegagent", "agent-110")
    if not code_agent:
        print("❌ Workflow failed at code agent creation step")
        return
    
    code_id = code_agent["id"]
    
    # Step 3: Create an agent test agent
    test_agent = create_agent("agenttest", "agent-111")
    if not test_agent:
        print("❌ Workflow failed at test agent creation step")
        return
    
    test_id = test_agent["id"]
    
    # Wait a moment to ensure agents are fully initialized
    print("\nWaiting for agents to initialize...")
    time.sleep(2)
    
    # Step 4: Register code agent as subagent of creator agent
    if not register_subagent(creator_id, code_id):
        print("❌ Workflow failed at registering code agent as subagent")
        return
    
    # Step 5: Register test agent as subagent of code agent
    if not register_subagent(code_id, test_id):
        print("❌ Workflow failed at registering test agent as subagent")
        return
    
    # Wait a moment to ensure agent relationships are established
    print("\nWaiting for agent relationships to be established...")
    time.sleep(2)
    
    # Step 6: Publish a message to the creator agent
    if not publish_message(creator_id, "Aevatar.Application.Grains.Agents.Code.PingMessage", "hello"):
        print("❌ Workflow failed at message publishing step")
        return
    
    print("\n✅ Agent setup workflow completed successfully!")
    print(f"Creator Agent ID: {creator_id}")
    print(f"Code Agent ID: {code_id}")
    print(f"Test Agent ID: {test_id}")

if __name__ == "__main__":
    run_workflow() 