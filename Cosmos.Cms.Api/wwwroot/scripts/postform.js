/**
 * Determines if an alement exists on the current page.
 * @param {any} element
 * @returns {boolean}
 */
function ccms___ElementExists(element) {
    if (typeof (element) === "undefined" || element === null || element === "") {
        return false;
    }
    return true;
}

/**
 * Posts a form and adds the Cosmos antiforgery validation token.
 * @param {any} endpoint
 * @param {any} form ID
 * @returns {Promise<Response>}
 */
async function ccms___PostForm(endpoint, formId) {
    // Find the form by it's ID.
    const form = document.getElementById(formId);
    if (!form) {
        console.error(`Form with ID "${formId}" not found.`);
        return;
    }

    // Collect form data
    const formData = new FormData(form);

    // Convert FormData to a plain object
    const data = {};
    formData.forEach((value, key) => {
        data[key] = value;
    });

    // Get the antiforgery token from the cookie.
    const value = `; ${document.cookie}`;
    const name = "X-XSRF-TOKEN";
    const parts = value.split(`; ${name}=`);
    const gRecaptcha = data['g-recaptcha-response'] || '';
    const hRecaptcha = data['h-captcha-response'] || '';

    // If the token is found then post the form.
    if (parts.length === 2) {
        const xsrfToken = parts.pop().split(';').shift();
        return fetch(endpoint, {
            method: "POST",
            headers: {
                'Content-Type': 'application/json',
                'X-XSRF-TOKEN': xsrfToken,
                'g-recaptcha-response': gRecaptcha,
                'h-captcha-response': hRecaptcha
            },
            body: JSON.stringify(data)
        }).then(data => {
            return data;
        });
    } else {
        throw new Error('X-XSRF-TOKEN cookie not found');
    }
}