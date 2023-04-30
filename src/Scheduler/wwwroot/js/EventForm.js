﻿/**
 * Provides navigation for the EventForm.
 * @param {string} eventType The type of event to select.
 */
function navClick(eventType) {
    const eventInputs = $('#eventInputs')

    
    refreshNav(eventType)
}


/**
 * Refreshes navbar to show the current event type as active.
 * @param {string} eventType The type of event to show as active.
 */
function refreshNav(eventType) {
    $('#Event').removeClass('active')
    $('#Practice').removeClass('active')
    $('#Game').removeClass('active')

    $(`#${eventType}`).addClass('active')

    $('#eventForm').attr('action', `../${eventType}/Schedule`)

    const title = $('#title')

    title.empty()
    title.html(eventType)
}

