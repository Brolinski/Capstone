$(() => {
    $('#recurrenceCheck').change(() => {
        const inputs = $('#recurrenceInputs')

        if ($('#recurrenceCheck').is(':checked')) {
            $.ajax({
                type: 'GET',
                url: '/Schedule/RenderInputs',
                data: { type: 'Recurrence' },
                success: (result) => {
                    inputs.empty()
                    inputs.html(result)
                }
            })

            return
        }

        inputs.empty()
    })
})