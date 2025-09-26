export function createEventSource(url, dotNetRef) {
    const eventSource = new EventSource(url);
    
    eventSource.onopen = function(event) {
        console.log('SSE connection opened');
        dotNetRef.invokeMethodAsync('OnSSEOpen');
    };
    
    eventSource.onmessage = function(event) {
        console.log('SSE message received:', event.data);
        dotNetRef.invokeMethodAsync('OnNotificationReceived', event.data);
    };
    
    eventSource.onerror = function(event) {
        console.error('SSE error:', event);
        dotNetRef.invokeMethodAsync('OnSSEError', 'Connection error');
    };
    
    return {
        close: function() {
            eventSource.close();
        }
    };
}